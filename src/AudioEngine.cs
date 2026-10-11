using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MRS;
public sealed class AudioEngine : IDisposable
{
    public const int Rate = 48000;
    private readonly object sync = new();
    private WaveInEvent? capture;
    private IWavePlayer? output;
    private MMDevice? endpoint;
    private string? endpointId;
    private BufferedWaveProvider? buffer;
    private WaveFileWriter? writer;
    private CancellationTokenSource? cancellation;
    private Task? playback;
    private float[] pending = new float[SignalMath.FftSize];
    private int pendingCount;
    private int generation;
    private int externalChannels = 1;
    private long processedSamples;
    internal event Action<byte[],int,int,double>? PcmReady;
    internal event Action? PcmBoundary;
    private WaveFormat RecordingFormat => new(Rate, 16, externalChannels);
    private static WaveFormat ListeningFormat => new(Rate, 16, 2);
    public volatile bool Monitor;
    public volatile float Volume = .35f;
    public volatile float Squelch = -60;
    public event Action<float[], double>? Frame;
    public event Action<string>? Ended;
    public bool Active { get; private set; }
    public static WaveFormat Format => new(Rate, 16, 1);
    internal void StartExternal(int channels = 1) { if (channels is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(channels)); Stop(); externalChannels = channels; Active = true; }
    internal void RetuneBoundary() { lock (sync) { pendingCount = 0; buffer?.ClearBuffer(); PcmBoundary?.Invoke(); } }
    internal void FeedExternal(byte[] pcm) { if (Active) Process(pcm, pcm.Length); }
    internal void FeedExternal(byte[] pcm, bool rfGateOpen) { if (Active) Process(pcm, pcm.Length, rfGateOpen); }
    internal void FeedStereo(byte[] pcm, bool rfGateOpen) { if (Active && externalChannels == 2) Process(pcm, pcm.Length, rfGateOpen, 2); }
    internal static byte[] DuplicateMono(byte[] pcm)
    {
        var result = new byte[pcm.Length * 2];
        for (int i = 0; i < pcm.Length / 2; i++) { result[i * 4] = result[i * 4 + 2] = pcm[i * 2]; result[i * 4 + 1] = result[i * 4 + 3] = pcm[i * 2 + 1]; }
        return result;
    }

    public void StartLive(int device)
    {
        Stop();
        try
        {
            capture = new WaveInEvent { DeviceNumber = device, WaveFormat = Format, BufferMilliseconds = 40 };
            int session = generation;
            capture.DataAvailable += (_, e) => { if (session == generation) Process(e.Buffer, e.BytesRecorded); };
            capture.RecordingStopped += (_, e) => { if (session == generation) Ended?.Invoke(e.Exception?.Message ?? "Audio input stopped."); };
            Active = true;
            capture.StartRecording();
        }
        catch { Stop(); throw; }
    }

    public void StartFile(string path)
    {
        Stop();
        // Open synchronously so unsupported files fail before the UI reports playback.
        var reader = new AudioFileReader(path);
        MediaFoundationResampler resampler;
        try { resampler = new MediaFoundationResampler(reader, Format) { ResamplerQuality = 60 }; }
        catch { reader.Dispose(); throw; }
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        int session = generation;
        Active = true;
        playback = Task.Run(async () =>
        {
            string message = "WAV playback finished.";
            try
            {
                using (reader)
                using (resampler)
                {
                    byte[] bytes = new byte[3840];
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    long total = 0;
                    while (!token.IsCancellationRequested)
                    {
                        int n = resampler.Read(bytes, 0, bytes.Length);
                        if (n == 0) break;
                        Process(bytes, n);
                        total += n;
                        int delay = (int)(total * 1000 / Format.AverageBytesPerSecond - clock.ElapsedMilliseconds);
                        if (delay > 0) await Task.Delay(delay, token);
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { message = "Playback error: " + ex.Message; }
            finally { if (session == generation) Active = false; }
            if (!token.IsCancellationRequested && session == generation) Ended?.Invoke(message);
        });
    }

    private void Process(byte[] bytes, int count, bool? rfGateOpen = null, int channels = 1)
    {
        try
        {
            lock (sync)
            {
                PcmReady?.Invoke(bytes,count,channels,processedSamples/(double)Rate);
                processedSamples+=count/(channels*2);
                writer?.Write(bytes, 0, count);
                var samples = new float[count / 2];
                for (int i = 0; i < samples.Length; i++) samples[i] = (short)(bytes[2 * i] | bytes[2 * i + 1] << 8) / 32768f;
                double level = SignalMath.Level(samples);
                if (Monitor && buffer != null)
                {
                    var listen = SignalMath.ListeningPcm(samples, level, Squelch, Volume, rfGateOpen);
                    if (channels == 1) listen = DuplicateMono(listen);
                    buffer.AddSamples(listen, 0, listen.Length);
                }
                for (int n = 0; n < samples.Length; n += channels)
                {
                    float sample = channels == 2 ? (samples[n] + samples[n + 1]) / 2 : samples[n];
                    pending[pendingCount++] = sample;
                    if (pendingCount == pending.Length)
                    {
                        Frame?.Invoke(SignalMath.Spectrum(pending), level);
                        pendingCount = 0;
                    }
                }
            }
        }
        catch (Exception ex) { Ended?.Invoke("Audio processing error: " + ex.Message); }
    }

    public void SetMonitoring(bool enabled, int device = -1)
    {
        lock (sync)
        {
            Monitor = false;
            if (enabled && output == null)
            {
                if (device < 0) throw new InvalidOperationException("Select a listening output first.");
                var candidate = new WaveOutEvent { DeviceNumber = device };
                var candidateBuffer = new BufferedWaveProvider(ListeningFormat) { BufferDuration = TimeSpan.FromMilliseconds(300), DiscardOnBufferOverflow = true };
                try { candidate.Init(candidateBuffer); candidate.Play(); output = candidate; buffer = candidateBuffer; }
                catch { candidate.Dispose(); throw; }
            }
            buffer?.ClearBuffer();
            if (!enabled) { output?.Dispose(); output = null; buffer = null; }
            Monitor = enabled;
        }
    }
    internal void SetEndpointMonitoring(bool enabled, string? id)
    {
        lock (sync)
        {
            if (enabled && output != null && endpointId == id) { Monitor = true; return; }
            Monitor = false; output?.Dispose(); output = null; buffer = null; endpoint?.Dispose(); endpoint = null; endpointId = null;
            if (!enabled) return;
            if (string.IsNullOrEmpty(id)) throw new InvalidOperationException("Select a listening output first.");
            using var devices = new MMDeviceEnumerator();
            var selected = devices.GetDevice(id);
            var candidate = new WasapiOut(selected, AudioClientShareMode.Shared, false, 100);
            var candidateBuffer = new BufferedWaveProvider(ListeningFormat) { BufferDuration = TimeSpan.FromMilliseconds(300), DiscardOnBufferOverflow = true };
            try { candidate.Init(candidateBuffer); candidate.Play(); endpoint = selected; endpointId = id; output = candidate; buffer = candidateBuffer; Monitor = true; }
            catch { candidate.Dispose(); selected.Dispose(); throw; }
        }
    }    public void Record(string path) { lock (sync) { if (!Active) throw new InvalidOperationException("Start a source first."); writer?.Dispose(); writer = new WaveFileWriter(path, RecordingFormat); } }
    public void FinishRecording() { lock (sync) { writer?.Dispose(); writer = null; } }
    public void Stop()
    {
        generation++;
        Active = false;
        cancellation?.Cancel();
        playback?.GetAwaiter().GetResult();
        playback = null;
        cancellation?.Dispose(); cancellation = null;
        capture?.StopRecording(); capture?.Dispose(); capture = null;
        lock (sync) { FinishRecording(); pendingCount = 0; buffer?.ClearBuffer(); externalChannels = 1; processedSamples=0; PcmBoundary?.Invoke(); }
    }
    public void Dispose() { Stop(); output?.Dispose(); output = null; endpoint?.Dispose(); endpoint = null; }
}
