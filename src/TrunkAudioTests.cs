using System.IO;
namespace MRS;
internal static class TrunkAudioTests
{
 internal static readonly byte[] ClearSymbols=VoiceFixtures.Hdu.Concat(VoiceFixtures.Ldu1).Concat(VoiceFixtures.Ldu2).ToArray();
 internal static TrunkCall Call=>new("P25",123,456,851025000,0,0x293,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,"Synthetic matched grant; independent OP25 voice waveform");
 internal static byte[] Terminator(int nac=0x293)
 {
  var output=VoiceFixtures.Hdu.Take(24).ToList();int physical=24;ulong nid=P25Framer.EncodeNid((ushort)((nac<<4)|3));
  for(int symbol=31;symbol>=0;symbol--){if(physical%36==35){output.Add(1);physical++;}output.Add((byte)((nid>>(symbol*2))&3));physical++;}
  output.AddRange(new byte[40]);return output.ToArray();
 }
 internal static async Task Feed(TrunkRadio radio,byte[] symbols,int rate=240000,bool signed=false,int centerOffset=0)
 {
  var bytes=P25Tests.Iq(1,rate,symbols);if(centerOffset!=0){for(int n=0;n<bytes.Length/2;n++){double phase=-2*Math.PI*centerOffset*n/rate,real=bytes[2*n]-127.5,imag=bytes[2*n+1]-127.5;bytes[2*n]=(byte)Math.Clamp(Math.Round(127.5+real*Math.Cos(phase)-imag*Math.Sin(phase)),0,255);bytes[2*n+1]=(byte)Math.Clamp(Math.Round(127.5+real*Math.Sin(phase)+imag*Math.Cos(phase)),0,255);}}
  if(signed)for(int n=0;n<bytes.Length;n++)bytes[n]^=128;
  const int block=32768;long start=radio.ProcessedBlocks;int count=(bytes.Length+block-1)/block;
  for(int offset=0;offset<bytes.Length;offset+=block)radio.Feed(new(offset,Call.Hz+centerOffset,rate,bytes.AsSpan(offset,Math.Min(block,bytes.Length-offset)).ToArray(),rate*.9,signed?IqSampleFormat.Signed8:IqSampleFormat.Unsigned8,new(Call.Hz,"NFM",16000)));
  for(int n=0;n<1000&&radio.ProcessedBlocks<start+count&&radio.Running;n++)await Task.Delay(10);
  if(radio.ProcessedBlocks<start+count||radio.Dropped!=0)throw new IOException($"Synthetic voice I/Q did not finish: {radio.ProcessedBlocks-start}/{count}, dropped {radio.Dropped}, {radio.ReceiverStatus}, {radio.VoiceStatus}");
 }
 internal static async Task Run(Action<bool,string> check)
 {
  using var radio=new TrunkRadio();radio.StartExternal(Call,"Synthetic control");await Feed(radio,ClearSymbols);var packets=radio.DrainAudio();
  check(packets.Length==2&&packets.Sum(p=>p.Pcm.Length)==18*160*6*4,"Dedicated trunk I/Q path recovers the independent 18-frame clear P25 voice fixture");
  check(packets.All(p=>p.Hz==Call.Hz&&p.Protocol=="P25"&&p.Key.Contains(":123:")),"Decoded trunk audio retains the assigned TG and voice frequency");
  check(packets.SelectMany(p=>p.Pcm).Any(b=>b!=0)&&radio.AudioFrames==18*160*6,"Followed clear voice contains synthesized PCM and updates decoded sample counts");
  await Feed(radio,Terminator());lock(radio.Sync)check(radio.Model.Active==null&&radio.Model.Events.Any(e=>e.Kind=="Call end"&&e.Detail=="P25 terminator"&&e.Talkgroup==123),"A generated P25 terminator ends the actual followed I/Q call with its TG retained");
  radio.StartExternal(Call with{Talkgroup=124},"Synthetic control");await Feed(radio,ClearSymbols);check(radio.DrainAudio().Length==0&&radio.AudioFrames==0,"Wrong talkgroup I/Q remains muted without stale audio from the previous call");
  radio.StartExternal(Call with{Nac=0x294},"Synthetic control");await Feed(radio,ClearSymbols);check(radio.DrainAudio().Length==0&&radio.AudioFrames==0&&radio.VoiceStatus.Contains("NAC mismatch"),"Wrong NAC I/Q cannot produce followed audio");
  radio.StartExternal(Call,"Synthetic control");await Feed(radio,VoiceFixtures.EncryptedHdu.Concat(VoiceFixtures.Ldu1).Concat(VoiceFixtures.EncryptedLdu2).ToArray());check(radio.DrainAudio().Length==0&&radio.AudioFrames==0,"Encrypted P25 fixture remains muted through the complete trunk I/Q worker");
  radio.StartExternal(Call,"Synthetic control");await Feed(radio,VoiceFixtures.Ldu1);check(radio.DrainAudio().Length==0&&radio.AudioFrames==0,"Voice I/Q without encryption metadata cannot create a decoded-audio success");
  radio.StartExternal(Call,"Synthetic control");await Feed(radio,ClearSymbols,960000,true,250000);packets=radio.DrainAudio();check(packets.Sum(p=>p.Pcm.Length)==18*160*6*4,"Signed I/Q with +250 kHz capture offset follows the correct digital voice channel");
  radio.StartExternal(Call,"Synthetic control");await Feed(radio,ClearSymbols);radio.Stop("Synthetic stop before audio drain");check(radio.DrainAudio().Length==0&&!radio.Running,"Stopping a followed call discards queued audio rather than playing it under a later assignment");
 }
}
