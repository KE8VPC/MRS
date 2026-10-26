using System.Globalization;
using System.Text.Json;
namespace MRS;
internal sealed record SensorResult(DateTimeOffset Seen,PacketContext Context,string Model,string Id,string Integrity,string Summary,string Json)
{
    internal string Key=>$"{Context.Key}:{Context.Replay}:{Context.Source}:{Context.FrequencyHz}:{Model}:{Id}"+(Id=="unknown"?":"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Json))):"");
    internal static SensorResult? Parse(string line,PacketContext context)
    {
        if(line.Length>65536)return null;
        try{using var doc=JsonDocument.Parse(line,new JsonDocumentOptions{MaxDepth=24});var r=doc.RootElement;
            if(r.ValueKind!=JsonValueKind.Object||!r.TryGetProperty("model",out var model)||model.ValueKind!=JsonValueKind.String)return null;
            string Value(string name)=>r.TryGetProperty(name,out var v)?v.ToString():"";
            string id=Value("id");if(id.Length==0)id=Value("device_id");if(id.Length==0)id=Value("channel");if(id.Length==0)id="unknown";
            var fields=r.EnumerateObject().Where(p=>p.Name is not ("time" or "model" or "id" or "protocol" or "mic")).Take(40).Select(p=>$"{p.Name}: {p.Value}");
            double? pos=context.PositionSeconds;if(r.TryGetProperty("time",out var time)&&time.ValueKind==JsonValueKind.Number&&time.TryGetDouble(out double seconds)&&double.IsFinite(seconds)&&seconds>=0&&seconds<=604800&&pos.HasValue)context=context with{PositionSeconds=pos+seconds};
            if(r.TryGetProperty("time",out var relative)&&relative.ValueKind==JsonValueKind.String&&relative.GetString() is {} ts&&ts.StartsWith('@')&&ts.EndsWith('s')&&double.TryParse(ts[1..^1],NumberStyles.Float,CultureInfo.InvariantCulture,out var elapsed)&&double.IsFinite(elapsed)&&elapsed>=0&&elapsed<=604800&&pos.HasValue)context=context with{PositionSeconds=pos+elapsed};
            string mic=Value("mic");return new(DateTimeOffset.UtcNow,context,model.GetString()??"unknown",id,mic.Length==0?"Decoder match; integrity unspecified":"Backend check: "+mic,string.Join(" · ",fields),line);
        }catch(JsonException){return null;}
    }
}
internal sealed record BurstResult(double Seconds,double DurationMs,int PulseCount,double MinUs,double MedianUs,double MaxUs,double ThresholdDb,double[] Timings,string Kind,string Detail,bool FirstHigh=true);
// OOK timing is experimental: threshold crossings are evidence, not decoded bits.
internal sealed class BurstInspector(int rate,double margin=8,bool narrow=false)
{
    private readonly List<double> timings=[];private readonly Queue<double> warm=[];private long samples,run,begin;private bool high,inBurst,gateHigh,firstHigh;private double floor=-70,threshold=-62,thresholdPower=0.000001,envelope;private int quiet,powerCount;private double powerSum;private long clock;private double lastI,lastQ,fm;private readonly List<double> audio=[];
    internal event Action<BurstResult>? Burst;
    internal void Process(ReadOnlySpan<byte> bytes,bool floatInput=false)
    {
        int stride=floatInput?8:2;for(int k=0;k+stride<=bytes.Length;k+=stride){double i=floatInput?BitConverter.ToSingle(bytes.Slice(k,4)):(bytes[k]-127.5)/128.0,q=floatInput?BitConverter.ToSingle(bytes.Slice(k+4,4)):(bytes[k+1]-127.5)/128.0,p=i*i+q*q;envelope+=Math.Min(1,(narrow?500.0:100000.0)/rate)*(p-envelope);powerSum+=p;powerCount++;
            // 1 ms envelope bins estimate quiet floor before triggering; bursts do not raise it.
            if(powerCount>=Math.Max(1,rate/1000)){double averaged=powerSum/powerCount;powerSum=0;powerCount=0;double db=10*Math.Log10(averaged+1e-12);if(warm.Count<100){warm.Enqueue(db);floor=warm.Order().ElementAt(warm.Count/4);}else if(!inBurst)floor+=Math.Clamp(db-floor,-.1,.01);threshold=floor+Math.Clamp(margin,3,30);thresholdPower=Math.Pow(10,threshold/10);gateHigh=averaged>=thresholdPower;}
            bool level=envelope>=thresholdPower;
            if(level!=high){if(inBurst&&timings.Count<4096)timings.Add(run*1e6/rate);run=0;high=level;}
            if(gateHigh){quiet=0;if(!inBurst&&warm.Count>=100){inBurst=true;begin=samples;run=0;firstHigh=level;high=level;timings.Clear();audio.Clear();}}
            else quiet++;
            if(inBurst){if(lastI!=0||lastQ!=0){double d=Math.Atan2(q*lastI-i*lastQ,i*lastI+q*lastQ)*rate/(2*Math.PI);fm+=Math.Min(1,2*Math.PI*4000/rate)*(d-fm);if(++clock>=Math.Max(1,rate/12000)){clock=0;if(audio.Count<120000)audio.Add(fm);}}
                if(quiet>=rate/(narrow?20:100)||samples-begin>=rate*5L||timings.Count>=4096)Finish();}
            lastI=i;lastQ=q;run++;samples++;
        }
    }
    internal void Finish(){if(!inBurst)return;inBurst=false;double duration=(samples-begin)/(double)rate;if(duration<.0002){timings.Clear();return;}var t=timings.Where(v=>v>0).Order().ToArray();
        // A small frequency-bank measurement is descriptive only; no vendor/protocol inference.
        var tones=new List<(int Hz,double Power)>();if(audio.Count>=2400){var a=audio.TakeLast(6000).ToArray();foreach(int hz in new[]{500,600,740,800,996,1180,1200,1500,1800,2200,2400}){double re=0,im=0;for(int n=0;n<a.Length;n++){double w=.5-.5*Math.Cos(2*Math.PI*n/(a.Length-1));re+=a[n]*w*Math.Cos(2*Math.PI*hz*n/(rate/(double)Math.Max(1,rate/12000)));im+=a[n]*w*Math.Sin(2*Math.PI*hz*n/(rate/(double)Math.Max(1,rate/12000)));}tones.Add((hz,re*re+im*im));}}
        string detail=tones.Count==0?"Short OOK/envelope burst; no validated protocol":"Strongest tested audio bins: "+string.Join(", ",tones.OrderByDescending(x=>x.Power).Take(3).Select(x=>$"{x.Hz} Hz"))+"; candidates only";
        Burst?.Invoke(new(begin/(double)rate,duration*1000,timings.Count,t.Length==0?0:t[0],t.Length==0?0:t[t.Length/2],t.Length==0?0:t[^1],threshold,timings.Take(2048).ToArray(),"Envelope / tone inspection",detail,firstHigh));timings.Clear();audio.Clear();quiet=0;}
}
