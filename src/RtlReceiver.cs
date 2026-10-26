using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace MRS;
internal sealed class RtlReceiver : IDisposable
{
    private Process? process;
    private Task? pump;
    private long lastSpectrum;
    private readonly RfSpectrumAnalyzer spectrumAnalyzer = new();
    internal ReceiverInputLevel? InputLevel;
    private long lastInputMeasurement;
    internal volatile int SpectrumSize = 32768;
    private volatile bool stopping;
    private CancellationTokenSource? cancellation;
    private readonly object recordingSync = new();
    private IqRecording? recording;
    internal bool IsReplay { get; private set; }
    internal string Backend { get; private set; } = "RTL";
    internal bool IsHackRf => Backend == "HackRF";
    internal string ReceiverName => IsHackRf ? "HackRF" : "RTL-SDR";
    internal bool CanTune(long hz) => IsHackRf ? ReceiverCapabilities.HackRf.CanTune(hz) : SamplingModes.CanTune(hz, SamplingMode);
    private IReadOnlyList<int> Rates => IsHackRf ? ReceiverCapabilities.HackRf.SampleRates : ReceiverCapabilities.Rtl.SampleRates;
    internal string GainDescription => IsHackRf ? HackRfGain.Decode(AppliedGain).ToString() : SamplingMode != 0 ? "N/A (direct sampling)" : AppliedGain == -1000 ? "Auto" : $"{AppliedGain / 10.0:0.0} dB";
    internal bool IsRecording { get { lock (recordingSync) return recording != null; } }
    internal long RecordedBytes { get { lock (recordingSync) return recording?.Bytes ?? 0; } }
    internal event Action<string>? RecordingStatus;
    internal event Action? ReplayGap;
    internal void RecordIq(string path, IqMetadata metadata)
    {
        if (IsReplay || AppliedFrequency == 0) throw new IOException("Start a live SDR source and wait for data before recording I/Q.");
        FinishIq();
        lock (recordingSync) recording = new IqRecording(path, metadata with { SamplingMode = SamplingMode, RequestedGain = RequestedGain, Backend = Backend });
    }
    internal void FinishIq()
    {
        IqRecording? finished; lock (recordingSync) { finished = recording; recording = null; }
        if (finished == null) return;
        try { finished.Dispose(); RecordingStatus?.Invoke(finished.Error ?? "I/Q file finalized."); }
        catch (Exception ex) { RecordingStatus?.Invoke("I/Q finalization failed: " + ex.Message); }
    }
    internal event Action<float[]>? Spectrum;
    internal event Action<byte[]>? Audio;
    internal event Action<IqFrame>? RawIq;
    internal event Action<IqFrame,double>? TriggeredIq;
    internal event Action<double[]>? DataSamples;
    internal volatile bool CaptureData;
    internal event Action<byte[]>? StereoAudio;
    internal volatile bool ForceMono;
    internal BroadcastState? Broadcast { get; private set; }
    internal RdsState? Rds { get; private set; }
    internal event Action<string>? Ended;
    internal event Action<string>? Status;
    internal volatile float SquelchThreshold = -60;
    internal volatile float ChannelDb = -120;
    internal volatile bool SquelchOpen;
    internal volatile bool SquelchEnabled = true;
    internal long AppliedFrequency { get; private set; }
    internal int SamplingMode { get; private set; }
    internal int AppliedRate { get; private set; }
    internal event Action<long, int>? Applied;
    private long requestedFrequency, tuneRequestedAt;
    private int requestedRate, requestedGain;
    private ChannelSettings? desiredChannel;
    private sealed record CaptureTune(long Center, ChannelSettings Channel);
    private CaptureTune? pendingCaptureTune;
    internal CaptureOffset Offset { get; private set; } = new(false);
    internal ChannelSettings? AppliedChannel { get; private set; }
    internal int AppliedGain { get; private set; } = -1000;
    internal int RequestedGain { get; private set; } = -1000;
    internal double AppliedBandwidth { get; private set; }
    internal event Action? ChannelChanged;
    internal void SetChannel(ChannelSettings channel)
    {
        channel.Validate();
        if (IsReplay) throw new IOException("Replay restores recorded channel settings.");
        if (!CanTune(channel.Frequency)) throw new IOException("Channel exceeds receiver tuning range.");
        if (AppliedFrequency == 0 || !channel.Fits(AppliedFrequency, AppliedBandwidth)) throw new IOException("Channel filter exceeds captured coverage; shift capture center first.");
        Volatile.Write(ref desiredChannel, channel);
    }
    internal Task SetRate(int rate)
    {
        if (IsHackRf && AppliedChannel is {} channel) Offset.Center(channel, rate, RequestedGain);
        return SendControl("RATE", rate);
    }
    internal Task SetGain(int gain)
    {
        if(IsHackRf && AppliedChannel is {} channel) Offset.Center(channel,AppliedRate,gain);
        return SendControl("GAIN", gain);
    }
    private async Task SendControl(string command, int value)
    {
        if (IsReplay || process == null) throw new IOException("Start a live receiver first.");
        if (command == "RATE" && !Rates.Contains(value)) throw new ArgumentOutOfRangeException(nameof(value));
        if (command == "GAIN") { if (IsHackRf) HackRfGain.Decode(value); else if (value != -1000 && (value < 0 || value > 600)) throw new ArgumentOutOfRangeException(nameof(value)); }
        if (command == "GAIN" && SamplingMode != 0) throw new IOException("Tuner gain is unavailable in direct sampling.");
        var target = process;
        await commandLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (process != target) throw new IOException("Receiver session changed.");
            if (command == "RATE") Volatile.Write(ref requestedRate, value); else Volatile.Write(ref requestedGain, value);
            Interlocked.Exchange(ref tuneRequestedAt, Environment.TickCount64);
            await target.StandardInput.WriteLineAsync(command + " " + value).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await target.StandardInput.FlushAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        finally { commandLock.Release(); }
    }
    internal int WorkerId => process?.Id ?? 0;
    private readonly SemaphoreSlim commandLock = new(1, 1);
    internal async Task Retune(long frequency, ChannelSettings? listeningChannel = null)
    {
        if (IsReplay) throw new IOException("I/Q replay follows the recorded frequencies; live retuning is unavailable.");
        if (!CanTune(frequency)) throw new ArgumentOutOfRangeException(nameof(frequency));
        var channel = listeningChannel ?? (AppliedChannel ?? throw new IOException("Wait for receiver data before tuning.")) with { Frequency = frequency };
        channel.Validate();
        if (IsHackRf) frequency = Offset.Center(channel, AppliedRate, RequestedGain);
        var targetWorker = process ?? throw new IOException("Receiver is not running.");
        await commandLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var worker = process ?? throw new IOException("Receiver is not running.");
            if (!ReferenceEquals(worker, targetWorker)) throw new IOException("Receiver session changed before tuning command could be sent.");
            Volatile.Write(ref pendingCaptureTune, new CaptureTune(frequency, channel));
            Interlocked.Exchange(ref requestedFrequency, frequency);
            Interlocked.Exchange(ref tuneRequestedAt, Environment.TickCount64);
            await worker.StandardInput.WriteLineAsync("TUNE " + frequency).WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            await worker.StandardInput.FlushAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }
        finally { commandLock.Release(); }
    }
    private static Process Launch(params string[] arguments)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        return Process.Start(start) ?? throw new IOException("Cannot start the receiver worker.");
    }
    internal static Task<List<RtlNative.Device>> Enumerate() => EnumerateBackend("RTL");
    internal static async Task<List<RtlNative.Device>> EnumerateBackend(string backend)
    {
        using var process = Launch(backend == "HackRF" ? "--hackrf-probe" : "--rtl-probe");
        try
        {
            var result = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(8));
            if (process.ExitCode != 0) throw new IOException(await error);
            return JsonSerializer.Deserialize<List<RtlNative.Device>>(await result) ?? [];
        }
        finally { if (!process.HasExited) process.Kill(true); }
    }
    internal static async Task InstallLibrary()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        byte[] zip = await client.GetByteArrayAsync("https://github.com/rtlsdrblog/rtl-sdr-blog/releases/download/v1.3.6/Release.zip");
        if (Convert.ToHexString(SHA256.HashData(zip)) != "A331691535FC99CFE21BC83B825C7B03F9F71022F93BC3F85254EB1D49FA77AB") throw new IOException("RTL library archive checksum did not match the pinned release.");
        Directory.CreateDirectory(RtlNative.LibraryFolder);
        using var archive = new ZipArchive(new MemoryStream(zip));
        foreach (string file in new[] { "rtlsdr.dll", "msvcr100.dll", "pthreadVC2.dll" })
        {
            var entry = archive.GetEntry("x64/" + file) ?? throw new IOException("Missing native library: " + file);
            string target = Path.Combine(RtlNative.LibraryFolder, file);
            entry.ExtractToFile(target + ".tmp", true);
            File.Move(target + ".tmp", target, true);
        }
    }
    internal void Start(uint index, long frequency, int gain, bool wide, bool fixture = false, int sampleRate = RtlNative.SampleRate, int fixtureSeconds = 1, string? identity = null, string? mode = null, double bandwidth = 0, bool agc = true, int samplingMode = 0, string backend = "RTL", CaptureOffset? captureOffset = null)
    {
        Stop();
        if (backend is not ("RTL" or "HackRF")) throw new ArgumentException("Unknown receiver backend.");
        Backend = backend; SamplingMode = samplingMode;
        Offset = IsHackRf ? captureOffset ?? new CaptureOffset(false) : new CaptureOffset(false);
        pendingCaptureTune = null;
        stopping = false;
        if (!Rates.Contains(sampleRate) || !CanTune(frequency) || IsHackRf && samplingMode != 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
        if (IsHackRf) HackRfGain.Decode(gain);
                Interlocked.Exchange(ref requestedFrequency, frequency); Interlocked.Exchange(ref tuneRequestedAt, 0);
        SamplingMode = samplingMode; if (samplingMode != 0) gain = -1000;
        requestedRate = sampleRate; requestedGain = gain;
        var initialChannel = new ChannelSettings(frequency, mode ?? (wide ? "WFM" : "NFM"), bandwidth, agc); initialChannel.Validate();
        if (IsHackRf) frequency = Offset.Center(initialChannel, sampleRate, gain);
        Interlocked.Exchange(ref requestedFrequency, frequency);
        desiredChannel = initialChannel;
        var worker = IsHackRf ? Launch(fixture ? "--hackrf-fixture" : "--hackrf-worker", index.ToString(), frequency.ToString(), gain.ToString(), sampleRate.ToString(), identity == null ? "" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identity)), fixtureSeconds.ToString()) : fixture ? Launch("--rtl-fixture", sampleRate.ToString(), fixtureSeconds.ToString(), gain.ToString(), samplingMode.ToString(), frequency.ToString()) : Launch("--rtl-worker", index.ToString(), frequency.ToString(), gain.ToString(), sampleRate.ToString(), identity == null ? "" : Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(identity)), samplingMode.ToString());
        process = worker;
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var selectedMode = mode ?? (wide ? "WFM" : "NFM");
        var dsp = new ReceiveDemodulator(selectedMode, sampleRate, bandwidth: initialChannel.Width, agc: initialChannel.Agc);
        var squelch = new RfSquelch();
        pump = Task.Run(async () =>
        {
            var errors = worker.StandardError.ReadToEndAsync();
            string message = "Receiver stopped.";
            try
            {
                byte[] header = new byte[IsHackRf ? HackRfProtocol.HeaderSize : ReceiverStream.LiveHeaderSize];
                bool first = true;
                while (!token.IsCancellationRequested)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeout.CancelAfter(first ? 10000 : 5000);
                    await worker.StandardOutput.BaseStream.ReadExactlyAsync(header, timeout.Token);
                    var metadata = IsHackRf ? HackRfProtocol.Header(header) : ReceiverStream.Header(header);
                    if (metadata.SamplingMode != SamplingMode) throw new IOException("Worker reported an unexpected sampling branch.");

                    byte[] block = new byte[metadata.Count];
                    await worker.StandardOutput.BaseStream.ReadExactlyAsync(block, timeout.Token);
                    if (metadata.Frequency != Interlocked.Read(ref requestedFrequency) || metadata.Rate != Volatile.Read(ref requestedRate) || metadata.RequestedGain != Volatile.Read(ref requestedGain))
                    {
                        long requestedAt = Interlocked.Read(ref tuneRequestedAt);
                        if (requestedAt != 0 && Environment.TickCount64 - requestedAt > 5000) throw new IOException("Receiver control change was not acknowledged within 5 seconds.");
                        continue;
                    }
                    long acknowledgedAt = Interlocked.Read(ref tuneRequestedAt);
                    bool captureChanged = first || metadata.Frequency != AppliedFrequency || metadata.Rate != AppliedRate || metadata.AppliedGain != AppliedGain || metadata.RequestedGain != RequestedGain;
                    var channel = Volatile.Read(ref desiredChannel) ?? initialChannel;
                    var pending = Volatile.Read(ref pendingCaptureTune);
                    if (pending != null && pending.Center == metadata.Frequency)
                    { channel = pending.Channel; Volatile.Write(ref desiredChannel, channel); Interlocked.CompareExchange(ref pendingCaptureTune, null, pending); }
                    if (!channel.Fits(metadata.Frequency, metadata.UsableBandwidth))
                    { channel = channel with { Frequency = metadata.Frequency }; Volatile.Write(ref desiredChannel, channel); }
                    bool channelChanged = channel != AppliedChannel;
                    if (captureChanged || channelChanged || acknowledgedAt != 0)
                    {
                        Interlocked.CompareExchange(ref tuneRequestedAt, 0, acknowledgedAt);
                        spectrumAnalyzer.Reset(); Broadcast = null; Rds = null; dsp = new ReceiveDemodulator(channel.Mode, metadata.Rate, channel.Frequency - (double)metadata.Frequency, channel.Width, channel.Agc); squelch = new RfSquelch();
                        AppliedFrequency = metadata.Frequency; AppliedRate = metadata.Rate; AppliedBandwidth = metadata.UsableBandwidth;
                        AppliedGain = metadata.AppliedGain; RequestedGain = metadata.RequestedGain; AppliedChannel = channel;
                        if (captureChanged) Applied?.Invoke(AppliedFrequency, AppliedRate);
                        ChannelChanged?.Invoke();
                        Status?.Invoke($"Receiving {channel.Mode} at {channel.Frequency / 1000000.0:F6} MHz · {AppliedRate / 1000.0:0} kS/s · {GainDescription}.");
                        first = false;
                    }                    bool recordingFailed = false;
                    lock (recordingSync) { if (recording != null) recordingFailed = !recording.Write(block, new CaptureDescriptor(AppliedFrequency, AppliedRate, AppliedBandwidth, IqSampleFormat.Unsigned8), AppliedChannel, AppliedGain, RequestedGain); }
                    if (recordingFailed) FinishIq();
                    RawIq?.Invoke(new IqFrame(0,AppliedFrequency,AppliedRate,block,AppliedBandwidth,IqSampleFormat.Unsigned8,AppliedChannel));
                    ProcessBlock(block, metadata.Rate, dsp, squelch);
                    TriggeredIq?.Invoke(new IqFrame(0,AppliedFrequency,AppliedRate,block,AppliedBandwidth,IqSampleFormat.Unsigned8,AppliedChannel,AppliedGain,RequestedGain),ChannelDb);
                }
            }
            catch (OperationCanceledException) { message = token.IsCancellationRequested ? "Receiver stopped." : "Receiver timed out. Check USB connection/driver and restart reception."; }
            catch (Exception ex) { message = ex.Message; }
            finally
            {
                try { if (stopping && IsHackRf && !worker.HasExited) await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromMilliseconds(700)); } catch { }
                try { if (!worker.HasExited) worker.Kill(true); await worker.WaitForExitAsync(); } catch { }
                string diagnostic = await errors;
                FinishIq();
                if (!token.IsCancellationRequested && !stopping) Ended?.Invoke(message + (string.IsNullOrWhiteSpace(diagnostic) ? "" : " " + diagnostic.Trim()));
            }
        });
    }
    internal double CwPitchHz { get; set; } = 700;
    private void ProcessBlock(byte[] block, int rate, ReceiveDemodulator dsp, RfSquelch squelch)
    {
        long inputNow=Environment.TickCount64;
        if(inputNow-lastInputMeasurement>=100){Volatile.Write(ref InputLevel,ReceiverInputLevel.Measure(block,inputNow));lastInputMeasurement=inputNow;}
        // Render at most 30 RF spectra per second; all samples still reach recording and DSP.
        spectrumAnalyzer.Append(block, SpectrumSize);
        if (Environment.TickCount64 - lastSpectrum >= 33) { lastSpectrum = Environment.TickCount64; if (spectrumAnalyzer.Snapshot() is {} spectrum) Spectrum?.Invoke(spectrum); }
        dsp.CwPitchHz = CwPitchHz;
        dsp.ForceMono = ForceMono;
        dsp.CaptureData = CaptureData;
        byte[] audio = dsp.Process(block); ChannelDb = (float)dsp.ChannelDb;
        if(dsp.DataSamples.Length>0)DataSamples?.Invoke(dsp.DataSamples);
        Broadcast = dsp.Broadcast;
        Rds = dsp.Rds;
        SquelchOpen = SquelchEnabled ? squelch.Update(ChannelDb, Math.Max(-119.9, SquelchThreshold), block.Length / (2.0 * rate)) : squelch.Update(ChannelDb, -120, block.Length / (2.0 * rate));
        Audio?.Invoke(audio);
        StereoAudio?.Invoke(dsp.Broadcast != null ? dsp.StereoAudio : AudioEngine.DuplicateMono(audio));
    }
    internal volatile bool ReplayPaused;
    internal volatile bool ReplayFinished;
    internal volatile bool ReplayReady;
    private long replayPosition, replayDuration, seekTicks = -1;
    internal TimeSpan ReplayPosition => TimeSpan.FromTicks(Interlocked.Read(ref replayPosition));
    internal TimeSpan ReplayDuration => TimeSpan.FromTicks(Interlocked.Read(ref replayDuration));
    internal void SeekReplay(TimeSpan position)
    {
        if (!IsReplay || !ReplayReady) throw new IOException("Wait for I/Q indexing to finish.");
        Interlocked.Exchange(ref seekTicks, Math.Clamp(position.Ticks, 0, Math.Max(0, Interlocked.Read(ref replayDuration) - 1)));
    }
    internal IqMetadata StartReplay(string path, bool paused = false)
    {
        Stop(); var reader = new IqReader(path); IsReplay = true; SamplingMode = reader.Metadata.SamplingMode; Backend = reader.Metadata.Backend;
        ReplayPaused = paused; ReplayFinished = false; ReplayReady = false; replayPosition = replayDuration = 0; seekTicks = -1;
        cancellation = new CancellationTokenSource(); var token = cancellation.Token;
        pump = Task.Run(async () =>
        {
            string message = "I/Q replay finished.";
            using (reader)
            {
                try
                {
                    Status?.Invoke("Indexing I/Q recording for playback and seeking…");
                    var index = IqReplayIndex.Build(reader, token);
                    Interlocked.Exchange(ref replayDuration, index.DurationTicks); ReplayReady = true;
                    if (index.TailError != null) Status?.Invoke("Recording has an incomplete or invalid tail: " + index.TailError);
                    var clock = Stopwatch.StartNew(); long baseTicks = 0, targetTicks = 0;
                    ReceiveDemodulator? dsp = null; var squelch = new RfSquelch();
                    IqFrame? frame = null;
                    void ApplyFrame(IqFrame value)
                    {
                        value.Capture.RequireCurrentDsp();
                        var channel = value.Channel ?? new ChannelSettings(value.Frequency, reader.Metadata.Mode ?? (reader.Metadata.WideFm ? "WFM" : "NFM"));
                        channel.Validate();
                        if (!channel.Fits(value.Frequency, value.Capture.UsableBandwidthHz)) throw new IOException("Replay channel exceeds recorded usable bandwidth.");
                        bool changed = AppliedFrequency != value.Frequency || AppliedRate != value.Rate || AppliedBandwidth != value.Capture.UsableBandwidthHz;
                        if (dsp == null || changed || AppliedChannel != channel)
                        {
                            spectrumAnalyzer.Reset(); Broadcast = null; Rds = null; dsp = new ReceiveDemodulator(channel.Mode, value.Rate, channel.Frequency - (double)value.Frequency, channel.Width, channel.Agc); squelch = new RfSquelch();
                            AppliedFrequency = value.Frequency; AppliedRate = value.Rate; AppliedBandwidth = value.Capture.UsableBandwidthHz;
                            AppliedChannel = channel; AppliedGain = value.Gain ?? reader.Metadata.RequestedGain; RequestedGain = value.RequestedGain ?? reader.Metadata.RequestedGain;
                            if (changed) Applied?.Invoke(AppliedFrequency, AppliedRate);
                            ChannelChanged?.Invoke();
                        }
                    }
                    while (!token.IsCancellationRequested)
                    {
                        long seek = Interlocked.Exchange(ref seekTicks, -1);
                        if (seek >= 0)
                        {
                            seek = Math.Min(seek, index.LastFrameTicks - index.FirstTicks);
                            targetTicks = seek; baseTicks = seek; clock.Restart(); if (ReplayPaused) clock.Stop();
                            reader.SeekFrame(index.OffsetFor(seek)); frame = null; dsp = null;
                            Interlocked.Exchange(ref replayPosition, seek); ReplayGap?.Invoke();
                            do { frame = reader.Read(); } while (frame != null && frame.ElapsedTicks - index.FirstTicks < seek);
                            if (frame != null) ApplyFrame(frame);
                        }
                        if (ReplayPaused) { clock.Stop(); await Task.Delay(20, token); continue; }
                        if (!clock.IsRunning) clock.Start();
                        frame ??= reader.Read(); if (frame == null) break;
                        frame.Capture.RequireCurrentDsp();
                        long relative = frame.ElapsedTicks - index.FirstTicks;
                        if (relative < targetTicks) { frame = null; continue; }
                        long remaining = relative - (baseTicks + clock.Elapsed.Ticks);
                        if (remaining > 0)
                        {
                            if (remaining > TimeSpan.FromMilliseconds(250).Ticks && dsp != null) { SquelchOpen = false; dsp = null; ReplayGap?.Invoke(); }
                            Interlocked.Exchange(ref replayPosition, Math.Min(index.DurationTicks, baseTicks + clock.Elapsed.Ticks));
                            await Task.Delay(Math.Min(20, Math.Max(1, (int)(remaining / TimeSpan.TicksPerMillisecond))), token); continue;
                        }
                        ApplyFrame(frame);
                        Interlocked.Exchange(ref replayPosition, relative);
                        RawIq?.Invoke(frame with {Channel=AppliedChannel});
                        ProcessBlock(frame.Data, frame.Rate, dsp!, squelch); frame = null;
                    }
                    Interlocked.Exchange(ref replayPosition, index.DurationTicks);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) { message = "I/Q replay stopped: " + ex.Message; }
            }
            ReplayFinished = true;
            if (!token.IsCancellationRequested) Ended?.Invoke(message);
        });
        return reader.Metadata;
    }    internal void Stop()
    {
        stopping = true;
        if (IsHackRf && process is { HasExited: false })
            try { process.StandardInput.WriteLineAsync("STOP").Wait(100); process.StandardInput.FlushAsync().Wait(100); } catch { }
        cancellation?.Cancel();
        // Kill only this owned worker. OS teardown releases USB resources even if the native read hung.
        try { if (!IsHackRf && process is { HasExited: false }) process.Kill(true); } catch (InvalidOperationException) { }
        pump?.GetAwaiter().GetResult();
        pump = null; process?.Dispose(); process = null;
        cancellation?.Dispose(); cancellation = null;
        FinishIq(); IsReplay = false; ReplayPaused = false; ReplayFinished = false; ReplayReady = false;
        spectrumAnalyzer.Reset(); lastSpectrum = 0; Volatile.Write(ref InputLevel,null); lastInputMeasurement=0;
        ChannelDb = -120; SquelchOpen = false; AppliedFrequency = 0; AppliedRate = 0; AppliedChannel = null; desiredChannel = null; Broadcast = null; Rds = null;
    }
    public void Dispose() => Stop();
}

