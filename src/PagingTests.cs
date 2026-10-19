using System.IO;
using NAudio.Wave;
namespace MRS;
internal static class PagingTests
{
    internal static void Run(Action<bool,string> check)
    {
        var source=new PacketContext("test","Synthetic paging",true,929937500,1.5);
        var p=PagingJson.Parse("{\"demod_name\":\"POCSAG1200\",\"address\":1234567,\"function\":3,\"alpha\":\"MRS TEST\",\"sync\":true}",source);
        check(p is {IsValidated:true,Protocol:"POCSAG",To:"1234567",Replay:true,PositionSeconds:1.5} && p.Information.Contains("1200 baud") && p.DecoderJson!=null && p.Description=="MRS TEST","Paging POCSAG fields and replay provenance; BCH is not claimed as a message CRC");
        check(PagingJson.Parse("{\"demod_name\":\"POCSAG512\",\"address\":null,\"alpha\":\"partial\",\"sync\":false}",source) is {IsValidated:false},"Partial POCSAG cannot become a validated page");
        const string flex="{\"baud\":1600,\"level\":4,\"msg_type\":\"alphanumeric\",\"capcode\":1234567,\"fragment\":\"fragment_start\",\"message\":\"MRS TEST\",\"k_ok\":true}";
        check(PagingJson.Parse(flex,source) is {IsValidated:true} f && f.Kind.Contains("fragment_start") && f.Information.Contains("1600 symbols/s"),"FLEX checksum-passing fragments stay explicitly labelled fragments, with symbol rate");
        check(PagingJson.Parse(flex.Replace("true","false"),source) is {IsValidated:false},"FLEX failed checksum stays unvalidated");
        check(PagingJson.Parse(flex.Replace("\"k_ok\":true","\"k_ok\":true,\"sig_ok\":false"),source) is {IsValidated:false},"FLEX signature failure overrides fragment checksum");
        check(PagingJson.Parse("{\"demod_name\":\"POCSAG1200\",\"address\":0,\"function\":0,\"sync\":true}",source) is {IsValidated:false},"Address-only POCSAG stays unvalidated, avoiding silence-generated tone-page floods");
        check(PagingJson.Parse("[]",source)==null && PagingJson.Parse("garbage",source)==null && PagingJson.Parse("{\"baud\":1600,\"msg_type\":\"bch_stats\"}",source)==null,"Paging ignores malformed output and diagnostic-only frames");
        var tone=Enumerable.Range(0,48000).Select(n=>2000*Math.Sin(n*2*Math.PI*1200/48000)).ToArray();
        var whole=new PagingResampler().Convert(tone);var split=new PagingResampler();
        var parts=split.Convert(tone.AsSpan(0,731)).Concat(split.Convert(tone.AsSpan(731))).ToArray();
        check(whole.Length==44100 && whole.SequenceEqual(parts),"Paging resampler preserves 22050 Hz timing across arbitrary callback splits");
        using var pipeline=new PagingPipeline();pipeline.Feed(tone,source);
        check(pipeline.Drain().Length==0 && pipeline.Status=="Paging off","Disabled paging never starts a worker or emits observations");
    }
    internal static async Task<string> Fixtures(string folder)
    {
        var report=new List<string>();
        foreach(string file in Directory.GetFiles(folder,"synthetic-*.wav"))
        {
            using var decoder=new PagingPipeline{Enabled=true};using var wav=new WaveFileReader(file);
            if(wav.WaveFormat.SampleRate!=48000 || wav.WaveFormat.BitsPerSample!=16)throw new IOException("Fixture requires 48k PCM16");
            var context=new PacketContext(file,"Generated paging",true,929937500);
            var bytes=new byte[9600];int count;
            while((count=wav.Read(bytes,0,bytes.Length))>0){decoder.FeedPcm(bytes.AsSpan(0,count),wav.WaveFormat.Channels,context with{PositionSeconds=wav.CurrentTime.TotalSeconds});await Task.Delay(5);}
            await decoder.CompleteAsync().WaitAsync(TimeSpan.FromSeconds(15));
            var rows=decoder.Drain(256);bool noise=Path.GetFileName(file).Contains("noise");
            if(noise?rows.Any(r=>r.IsValidated):!rows.Any(r=>r.IsValidated && r.To=="1234567" && (r.Information.Contains("MRS")||r.Information.Contains("9876543210"))))throw new InvalidOperationException($"Paging fixture failed: {Path.GetFileName(file)}; rows {rows.Length}; {decoder.Status}");
            if(rows.Count(r=>r.IsValidated)>1)throw new InvalidOperationException("Unexpected extra validated pages in single-page fixture");
            if(rows.Any(r=>!r.Replay||r.FrequencyHz!=929937500||r.SourceKey!=file))throw new InvalidOperationException("Paging source attribution lost");
            report.Add($"PASS: {Path.GetFileName(file)}; {rows.Length} observations, {rows.Count(r=>r.IsValidated)} validated");
        }
        if(report.Count<6)throw new IOException("Missing paging fixtures");
        return string.Join(Environment.NewLine,report);
    }
    internal static async Task<string> Iq(string file)
    {
        using var reader=new IqReader(file);using var decoder=new PagingPipeline{Enabled=true};
        ReceiveDemodulator? dsp=null;long frames=0;
        while(reader.Read() is {} frame)
        {
            var ch=frame.Channel??throw new IOException("No channel metadata");
            dsp??=new ReceiveDemodulator("NFM",frame.Rate,ch.Frequency-frame.Frequency,ch.Width){CaptureData=true};
            dsp.Process(frame.Data);decoder.Feed(dsp.DataSamples,new PacketContext("local-iq","Local I/Q validation",true,ch.Frequency,frame.ElapsedTicks/(double)TimeSpan.TicksPerSecond));
            if(++frames%8==0)await Task.Delay(1);
        }
        await decoder.CompleteAsync().WaitAsync(TimeSpan.FromSeconds(15));var rows=decoder.Drain(256);
        // Report counts only; private page contents do not belong in build logs.
        return $"Frames {frames}; observations {rows.Length}; FLEX {rows.Count(r=>r.Protocol=="FLEX")}; checksum-passing {rows.Count(r=>r.Protocol=="FLEX"&&r.IsValidated)}; POCSAG {rows.Count(r=>r.Protocol=="POCSAG")}; {decoder.Status}";
    }
}
