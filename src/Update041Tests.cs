using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
namespace MRS;
internal static class Update041Tests
{
 internal static void Run(Action<bool,string> check)
 {
  var c=new PacketContext("041-test","Synthetic test",true,146520000,2.5);
  var same=RadioDataParse.Signaling("{\"demod_name\":\"EAS\",\"header_begin\":\"ZCZC\",\"last_message\":\"-WXR-RWT-039011+0015-3021200-K1ABC   -\"}",c)!;
  check(same.IsValidated&&same.Information.Contains("Required Weekly Test")&&same.Information.Contains("039011")&&same.Replay,"SAME decodes structured event, county code, time and replay evidence");
  check(!RadioDataParse.Signaling("{\"demod_name\":\"DTMF\",\"digit\":\"#\"}",c)!.IsValidated,"DTMF tone candidates are not called checksum-validated");
  check(RadioDataParse.Signaling("{\"demod_name\":\"DTMF\",\"digit\":\"bogus\"}",c)==null,"Malformed tone results rejected");
  check(!RadioDataParse.Signaling("{\"demod_name\":\"EAS\",\"header_begin\":\"ZCZC\",\"last_message\":\"bad\"}",c)!.IsValidated,"Malformed SAME header cannot become validated alert");
  check(!MeshPacket.Data([8,255,255,255,255,127,18,0],out _,out _),"Mesh protobuf rejects overflowing port identifiers");
  check(!MeshPacket.Data([18,255],out _,out _),"Truncated mesh protobuf rejected");
  var options=new MeshOptions("Meshtastic",906875000);
  byte[] header=Convert.FromHexString("FFFFFFFF785634122A00000003080000"),plain=[8,1,18,3,77,82,83];
  byte[] raw=header.Concat(plain).ToArray(),key=Convert.FromHexString("D4F1BBCF01F6A5E78A50E27B22E7D56B");
  byte[] cipher=MeshPacket.MeshtasticPlain(raw,key);raw=header.Concat(cipher).ToArray();
  check(MeshPacket.MeshtasticPlain(raw,key).SequenceEqual(plain),"Meshtastic public AES-CTR nonce/packet payload round-trip");
  string Line(byte[] b,bool good=true,int sync=43)=>JsonSerializer.Serialize(new{status=2,has_crc=true,crc_ok=good,sync,snr_db=10,hex=Convert.ToHexString(b)});
  var mesh=MeshPacket.Parse(Line(raw),c,options)!;
  check(mesh.Callsign=="!12345678"&&mesh.Information.Contains("Text: MRS")&&mesh.Validation.Contains("no payload authentication"),"Public Meshtastic text decoded with identity and authentication limitation");
  check(!MeshPacket.Parse(Line(raw,false),c,options)!.Information.Contains("Text: MRS"),"CRC-failed LoRa cannot supply mesh application text");
  check(!MeshPacket.Parse(Line(raw,true,18),c,options)!.IsValidated,"Wrong LoRa sync remains separate unverified evidence");
  check(!MeshPacket.Parse(Line(raw),c,options with{PublicText=false})!.Information.Contains("Text: MRS"),"Public payload decoding toggle preserves opaque envelope");
  byte[] coreKey=Convert.FromBase64String("izOH6cXN6mrJ5e26oRXNcg=="),corePlain=new byte[16];Encoding.UTF8.GetBytes("MRS public").CopyTo(corePlain,5);
  using(var aes=Aes.Create()){aes.Key=coreKey;var encrypted=aes.EncryptEcb(corePlain,PaddingMode.None);var mac=HMACSHA256.HashData(coreKey,encrypted);byte[] frame=new byte[]{0x15,0,SHA256.HashData(coreKey)[0],mac[0],mac[1]}.Concat(encrypted).ToArray();var row=MeshPacket.Parse(Line(frame,true,18),c,new("MeshCore",915000000,8,62500,4,18))!;check(row.Information.Contains("Public: MRS public")&&row.Validation.Contains("16-bit HMAC"),"MeshCore Public channel text and short MAC verified");frame[3]^=1;check(!MeshPacket.Parse(Line(frame,true,18),c,new("MeshCore",915000000,8,62500,4,18))!.Information.Contains("Public: MRS public"),"MeshCore corrupted public MAC does not expose claimed text");}
  foreach(var (mode,file,expected) in new[]{("Signaling","same","RWT"),("Signaling","dtmf","#"),("ANI","mdc1200","4660"),("ANI","fleetsync","100-1290"),("FT8","ft8","CQ K1ABC FN42"),("FT4","ft4","CQ K1ABC FN42"),("WSPR","wspr","K1ABC FN42 30")})
  {
   using var pipeline=new RadioDataPipeline();pipeline.Configure(mode);using var wav=new WaveFileReader(Path.Combine(AppContext.BaseDirectory,"Docs","Samples","synthetic-"+file+".wav"));var audio=new WdlResamplingSampleProvider(wav.ToSampleProvider(),48000);float[] floats=new float[4800];long total=0;int count;
   while((count=audio.Read(floats,0,floats.Length))>0){double[] v=floats.AsSpan(0,count).ToArray().Select(x=>(double)x*32768).ToArray();pipeline.Feed(v,c with{PositionSeconds=total/48000.0},total/48000.0);total+=count;Thread.Sleep(2);}
   pipeline.Complete();var rows=new List<PacketObservation>();var until=DateTime.UtcNow.AddSeconds(35);do{rows.AddRange(pipeline.Drain());if(rows.Any(x=>x.Information.Contains(expected)))break;Thread.Sleep(40);}while(DateTime.UtcNow<until);
   check(rows.Any(x=>x.Information.Contains(expected)&&x.Replay&&x.SourceKey==c.Key),$"Integrated {file} audio replay decodes expected message and source context ({pipeline.Status})");
   pipeline.Reset();check(pipeline.Status.Contains("Source changed"),$"{file} seek/retune resets decoder acquisition");
  }
  using(var radio=new MeshRadio()){radio.Start(options);byte[] iq=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Docs","Samples","synthetic-lora.cf32"));byte[] bytes=new byte[iq.Length/2];for(int n=0;n<iq.Length/8;n++){float re=BitConverter.ToSingle(iq,n*8),im=BitConverter.ToSingle(iq,n*8+4);for(int k=0;k<2;k++){bytes[n*4+k*2]=(byte)Math.Clamp(128+re*80,0,255);bytes[n*4+k*2+1]=(byte)Math.Clamp(128+im*80,0,255);}}for(int at=0;at<bytes.Length;at+=16384){radio.Feed(new(0,options.Frequency,1000000,bytes.AsSpan(at,Math.Min(16384,bytes.Length-at)).ToArray()),c);Thread.Sleep(2);}radio.Complete();var rows=new List<PacketObservation>();var until=DateTime.UtcNow.AddSeconds(20);do{rows.AddRange(radio.Drain());if(rows.Any(r=>r.IsValidated))break;Thread.Sleep(30);}while(DateTime.UtcNow<until);check(rows.Any(r=>r.IsValidated&&r.Callsign=="!12345678"&&r.Replay),"Direct IQ channel bridge and LoRa demodulator recover generated mesh packet ("+radio.Status+")");radio.Reset();check(radio.Status.Contains("Source changed"),"Mesh seek/retune reacquires a fresh capture");}
 }
}
