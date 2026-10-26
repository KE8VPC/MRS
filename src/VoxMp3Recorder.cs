using System.IO;
using System.Threading.Channels;
using NAudio.Wave;
namespace MRS;
internal sealed record VoxOptions(double ThresholdDb=-35,double PreSeconds=1,double TailSeconds=2,double MaximumSeconds=30,int BudgetMb=1024,bool Stereo=false)
{
    internal VoxOptions Normalize()=>new(double.IsFinite(ThresholdDb)?Math.Clamp(ThresholdDb,-80,-3):-35,Math.Clamp(double.IsFinite(PreSeconds)?PreSeconds:1,0,3),Math.Clamp(double.IsFinite(TailSeconds)?TailSeconds:2,.2,15),Math.Clamp(double.IsFinite(MaximumSeconds)?MaximumSeconds:30,1,300),Math.Clamp(BudgetMb,64,16384),Stereo);
}
internal sealed class VoxMp3Recorder:IDisposable
{
    private sealed record Run(string Folder,string Slot,VoxOptions Options);
    private sealed record Input(Run? Run,byte[]? Data,int Channels,string Key,long Hz,bool Reset=false,TaskCompletionSource? Done=null);
    private sealed record Job(string Wav,string Mp3,int Bitrate,long Bytes);
    private readonly object gate=new();private Run? run;private bool disposed;private readonly Channel<Input> input=Channel.CreateBounded<Input>(128);private readonly Channel<Job> jobs=Channel.CreateBounded<Job>(4);private readonly Task worker,encoder;
    private int closingRun;private string status="Disarmed";private long bytes,pending;private int clips;private long signalUntil;private string error="";
    internal bool Armed{get{lock(gate)return run!=null;}}internal bool SignalOpen=>Environment.TickCount64<Interlocked.Read(ref signalUntil);internal string Status=>Volatile.Read(ref error).Length>0?Volatile.Read(ref error):Volatile.Read(ref status);internal int Clips=>Volatile.Read(ref clips);internal long Bytes=>Interlocked.Read(ref bytes);
    internal VoxMp3Recorder(){worker=Task.Run(Work);encoder=Task.Run(Encode);}
    internal void Arm(string folder,string slot,VoxOptions options){Directory.CreateDirectory(folder);lock(gate){if(disposed)throw new ObjectDisposedException(nameof(VoxMp3Recorder));if(run!=null||Volatile.Read(ref closingRun)!=0||Interlocked.Read(ref pending)!=0)throw new IOException("Finish pending MP3 encoding before arming again.");run=new(Path.GetFullPath(folder),slot,options.Normalize());bytes=0;clips=0;error="";status="Armed · waiting for audio VOX";}}
    internal void Disarm(string reason="Disarmed"){lock(gate){run=null;signalUntil=0;status=reason;closingRun=1;if(!input.Writer.TryWrite(new(null,null,0,"",0,true)))closingRun=0;}}
    internal void Boundary(){Interlocked.Exchange(ref signalUntil,0);lock(gate){if(run==null)return;if(!input.Writer.TryWrite(new(run,null,0,"",0,true)))Fail("MP3 boundary queue overrun; partial audio retained");}}
    private void Fail(string reason){Volatile.Write(ref error,reason);Disarm("Stopped");}
    internal void Feed(byte[] pcm,int count,int channels,string key,long hz){lock(gate){if(run==null||disposed)return;if(channels is not (1 or 2)||count<=0||count>192000||count>pcm.Length||count%(channels*2)!=0){Fail("Invalid MP3 input block");return;}if(!input.Writer.TryWrite(new(run,pcm.AsSpan(0,count).ToArray(),channels,key,hz)))Fail("MP3 input overrun; complete audio retained");}}
    internal async Task DrainAsync(){var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);await input.Writer.WriteAsync(new(null,null,0,"",0,Done:done));await done.Task;}
    internal async Task FinishAsync(){await DrainAsync();Disarm();await DrainAsync();while(Interlocked.Read(ref pending)>0&&Volatile.Read(ref error).Length==0)await Task.Delay(20);}
    internal static byte[] ConvertPcm(byte[] data,int channels,bool stereo){int target=stereo?2:1;var result=new byte[data.Length/channels*target];int p=0;for(int n=0;n<data.Length;n+=channels*2){int l=BitConverter.ToInt16(data,n),r=channels==2?BitConverter.ToInt16(data,n+2):l;if(target==1){System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(p,2),(short)((l+r)/2));p+=2;}else{System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(p,2),(short)l);System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(result.AsSpan(p+2,2),(short)r);p+=4;}}return result;}
    internal static double Level(byte[] pcm){double sum=0;for(int n=0;n<pcm.Length;n+=2){double v=BitConverter.ToInt16(pcm,n)/32768.0;sum+=v*v;}return 10*Math.Log10(Math.Max(1e-12,sum/Math.Max(1,pcm.Length/2)));}
    private async Task Work()
    {
        Run? active=null;WaveFileWriter? writer=null;string wav="",mp3="",key="";long written=0;double duration=0,quiet=0,preSeconds=0;int channels=1;var pre=new Queue<byte[]>();long lastInput=Environment.TickCount64;long frequency=0;
        void Clear(){pre.Clear();preSeconds=0;key="";}
        void Finish(){if(writer==null)return;writer.Dispose();writer=null;var job=new Job(wav,mp3,channels==2?128000:64000,written);if(!jobs.Writer.TryWrite(job))throw new IOException("MP3 encoder backlog; retained WAV: "+wav);duration=quiet=0;written=0;}
        void Write(byte[] data){if(bytes+pending+data.Length>active!.Options.BudgetMb*1000000L)throw new IOException("MP3 storage budget reached; complete audio retained");writer!.Write(data,0,data.Length);Interlocked.Add(ref pending,data.Length);written+=data.Length;duration+=data.Length/(48000.0*channels*2);}
        void Process(byte[] pcm,bool real){var o=active!.Options;double seconds=pcm.Length/(48000.0*channels*2);bool signal=Level(pcm)>=o.ThresholdDb;if(signal)Interlocked.Exchange(ref signalUntil,Environment.TickCount64+100);
            if(writer==null&&signal){if(new DriveInfo(Path.GetPathRoot(active.Folder)!).AvailableFreeSpace<256000000)throw new IOException("Less than 256 MB free for MP3 recording");string stem=$"MRS-VOX-{active.Slot}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{frequency}-{Guid.NewGuid():N}";wav=Path.Combine(active.Folder,stem+".pending.wav");mp3=Path.Combine(active.Folder,stem+".mp3");writer=new(wav,new WaveFormat(48000,16,channels));while(pre.Count>0){var b=pre.Dequeue();if(duration+b.Length/(48000.0*channels*2)+seconds<=o.MaximumSeconds)Write(b);}preSeconds=0;status="Recording MP3 audio";}
            if(writer!=null){Write(pcm);quiet=signal?0:quiet+seconds;if(quiet>=o.TailSeconds||duration>=o.MaximumSeconds){Finish();status="Armed · waiting for audio VOX";}}
            else if(real){pre.Enqueue(pcm);preSeconds+=seconds;while(pre.Count>0&&preSeconds>o.PreSeconds){preSeconds-=pre.Dequeue().Length/(48000.0*channels*2);}}
        }
        try{while(true){if(!input.Reader.TryRead(out var item)){if(input.Reader.Completion.IsCompleted)break;using(var timeout=new CancellationTokenSource(100)){try{if(!await input.Reader.WaitToReadAsync(timeout.Token))break;}catch(OperationCanceledException){}}if(writer!=null&&Environment.TickCount64-lastInput>=100){try{Process(new byte[4800*channels*2],false);lastInput=Environment.TickCount64;}catch(Exception ex){Fail(ex.Message);Finish();Clear();active=null;}}continue;}
                try{if(item.Done!=null){item.Done.TrySetResult();continue;}var current=item.Run;if(item.Reset||active!=current){Finish();Clear();active=current;if(item.Reset&&current==null)Volatile.Write(ref closingRun,0);}if(item.Data==null||current==null||Volatile.Read(ref error).Length>0)continue;int target=current.Options.Stereo?2:1;if(key!=item.Key||channels!=target){Finish();Clear();key=item.Key;channels=target;}active=current;frequency=item.Hz;lastInput=Environment.TickCount64;var converted=ConvertPcm(item.Data,item.Channels,current.Options.Stereo);for(int offset=0;offset<converted.Length;offset+=1920*channels)Process(converted.AsSpan(offset,Math.Min(1920*channels,converted.Length-offset)).ToArray(),true);}
                catch(Exception ex){Fail(ex.Message);try{Finish();}catch(Exception e){Volatile.Write(ref error,e.Message);}Clear();active=null;item.Done?.TrySetResult();}}
        }finally{try{Finish();}catch(Exception ex){Volatile.Write(ref error,ex.Message);}jobs.Writer.TryComplete();}
    }
    private async Task Encode(){await foreach(var job in jobs.Reader.ReadAllAsync()){string part=job.Mp3+".encoding.mp3";try{using(var reader=new WaveFileReader(job.Wav))MediaFoundationEncoder.EncodeToMp3(reader,part,job.Bitrate);if(new FileInfo(part).Length==0)throw new IOException("MP3 encoder produced an empty file");File.Move(part,job.Mp3);Interlocked.Add(ref bytes,new FileInfo(job.Mp3).Length);File.Delete(job.Wav);Interlocked.Add(ref pending,-job.Bytes);Interlocked.Increment(ref clips);}catch(Exception ex){Fail("MP3 encoding failed: "+ex.Message+"; retained WAV: "+job.Wav);}}}
    public void Dispose(){lock(gate){if(disposed)return;disposed=true;run=null;input.Writer.TryComplete();}worker.GetAwaiter().GetResult();encoder.GetAwaiter().GetResult();}
}
