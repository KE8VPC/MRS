using System.Collections.Concurrent;
using System.Threading.Channels;
namespace MRS;

// Bounded raw-I/Q consumer, independent of squelch and the normal DMR observation panel.
internal sealed class LcnSurveyRadio:IDisposable
{
 private sealed record Target(long Epoch,long Hz,int? Slot);
 private readonly Channel<(Target Target,IqFrame Frame)> input=Channel.CreateBounded<(Target,IqFrame)>(64);
 private readonly ConcurrentQueue<(long Epoch,DmrReception Row)> output=[];private readonly ConcurrentQueue<(long Epoch,byte[] Pcm)> audio=[];
 private readonly DigitalVoicePipeline voice=new();private readonly Task worker;private Target target=new(0,0,null);private long serial;private int fault;
 internal string Error=>Volatile.Read(ref fault)==0?"":"Research decoder overrun or unsupported I/Q; stopped to preserve evidence accuracy";
 internal void Tune(long hz,int? slot=null){if(Volatile.Read(ref target) is {} old&&old.Hz==hz&&old.Slot==slot)return;var next=new Target(Interlocked.Increment(ref serial),hz,slot);Volatile.Write(ref target,next);voice.Configure(0,3,false);Interlocked.Exchange(ref fault,0);}
 internal LcnSurveyRadio(){worker=Task.Run(Work);}
 internal void Feed(IqFrame f){var t=Volatile.Read(ref target);if(t.Hz==0)return;if(f.Format is not(IqSampleFormat.Unsigned8 or IqSampleFormat.Signed8)||f.Data.Length>65536||!input.Writer.TryWrite((t,f with{Data=(byte[])f.Data.Clone()})))Interlocked.Exchange(ref fault,1);}
 private async Task Work()
 {
  long epoch=-1,center=0;int rate=0;ReceiveDemodulator? dsp=null;DmrDecoder decoder=new();List<double> samples=[];
  Action<byte[]>? audioHandler=null;
  await foreach(var item in input.Reader.ReadAllAsync()){
   var t=item.Target;if(t.Epoch!=Volatile.Read(ref target).Epoch||t.Hz==0)continue;var f=item.Frame;
   try{
    if(!new ChannelSettings(t.Hz,"NFM",12500).Fits(f.Frequency,f.Capture.UsableBandwidthHz))continue;
    if(epoch!=t.Epoch||center!=f.Frequency||rate!=f.Rate){epoch=t.Epoch;center=f.Frequency;rate=f.Rate;samples.Clear();dsp=new("NFM",rate,t.Hz-center,12500){CaptureData=true};decoder=new();long decoderEpoch=epoch;long hz=t.Hz;decoder.Message+=o=>{if(output.Count>=256){Interlocked.Exchange(ref fault,1);return;}output.Enqueue((decoderEpoch,new(o,new($"survey:{decoderEpoch}","LCN discovery",false,hz),DateTimeOffset.UtcNow)));};voice.Configure(0,3,false);if(audioHandler!=null)voice.Audio-=audioHandler;long audioEpoch=epoch;int slot=t.Slot??1;audioHandler=pcm=>{if(Volatile.Read(ref target).Epoch!=audioEpoch||audio.Count>=24)return;var mono=new byte[pcm.Length];for(int i=0;i+3<pcm.Length;i+=4){int p=i+(slot==2?2:0);mono[i]=mono[i+2]=pcm[p];mono[i+1]=mono[i+3]=pcm[p+1];}audio.Enqueue((audioEpoch,mono));};voice.Audio+=audioHandler;voice.Configure(t.Slot.HasValue?1:0,t.Slot??3,false);if(t.Epoch!=Volatile.Read(ref target).Epoch){voice.Configure(0,3,false);continue;}}
    byte[] bytes=f.Data;if(f.Format==IqSampleFormat.Signed8){bytes=(byte[])bytes.Clone();for(int i=0;i<bytes.Length;i++)bytes[i]^=128;}dsp!.Process(bytes);decoder.Process(dsp.DataSamples);
    if(t.Slot.HasValue&&t.Epoch==Volatile.Read(ref target).Epoch){samples.AddRange(dsp.DataSamples);while(samples.Count>=960){var block=samples.Take(960).ToArray();samples.RemoveRange(0,960);voice.Feed(block,new($"survey:{epoch}","LCN discovery voice",false,t.Hz));}}
   }catch{Interlocked.Exchange(ref fault,1);}
  }
 }
 internal DmrReception[] Drain(){var rows=new List<DmrReception>();while(output.TryDequeue(out var r))if(r.Epoch==Volatile.Read(ref target).Epoch)rows.Add(r.Row);return rows.ToArray();}
 internal byte[][] DrainAudio(){var rows=new List<byte[]>();while(audio.TryDequeue(out var r))if(r.Epoch==Volatile.Read(ref target).Epoch)rows.Add(r.Pcm);return rows.ToArray();}
 public void Dispose(){Tune(0);input.Writer.TryComplete();worker.GetAwaiter().GetResult();voice.Dispose();}
}
