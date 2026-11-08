using System.IO;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderTrunkAudioSmoke()
 {
  Stop();var original=fleetProfile;MainWindow? voice=null;int passed=0;
  async Task Wait(Func<bool> ready){for(int n=0;n<700&&!ready();n++)await Task.Delay(10);if(!ready())throw new IOException("Synthetic trunk voice state timed out");}
  try{
   fleetProfile=new(workspaceSlot,"Synthetic P25 control","Trunk control","Audio path test");ReflectFleet();
   voice=new MainWindow(true,"B",Path.Combine(dataFolder,"audio-test-voice")){ShowInTaskbar=false,Left=-16000};voice.fleetProfile=new("B","Synthetic P25 voice","Trunk voice","Audio path test");voice.ReflectFleet();voice.Show();
   voice.rfMode=true;voice.engine.StartExternal(2);voice.receiver.Start(0,TrunkAudioTests.Call.Hz,0,false,fixture:true,sampleRate:240000,fixtureSeconds:40);
   await Wait(()=>voice.receiver.AppliedFrequency>0);
   // Replace only the trunk input in this diagnostic workspace with the known
   // OP25 waveform; the receiver worker remains a synthetic tune-ack fixture.
   voice.receiver.RawIq-=voice.trunkRadio.Feed;
   trunkRadio.Start(new("audio-smoke","Synthetic control","P25",851012500,[]));trunkRadio.SuppressLocalVoice=true;
   lock(trunkRadio.Sync){trunkRadio.Model.HangSeconds=15;trunkRadio.Model.Labels[123]="Synthetic dispatch";}
   async Task Case(string name,byte[] symbols,int tg,int nac,bool expectAudio)
   {
    if(voice.fleetOwner!=null)voice.EndFleetVoice("Synthetic case boundary");
    var grant=Update039Tests.Grant(851012500,TrunkAudioTests.Call.Hz,tg);
    grant=grant with{P25Identity=grant.P25Identity! with{Nac=nac},P25Assignments=grant.P25Assignments!.Select(g=>g with{SourceRadioId=456}).ToArray()};
    lock(trunkRadio.Sync){trunkRadio.Model.Allowed=[tg];trunkRadio.Model.End(DateTimeOffset.UtcNow,"Next synthetic case");trunkRadio.Model.ObserveP25(grant,DateTimeOffset.UtcNow);}
    TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy&&voice.trunkRadio.Running);
    int ownBefore=voice.trunkDiscoveries.Count(e=>e.Kind=="Decoded audio"),controlBefore=trunkDiscoveries.Count(e=>e.Kind=="Decoded audio");
    await TrunkAudioTests.Feed(voice.trunkRadio,symbols);voice.TickTrunk();voice.TickTrunk();
    int ownAfter=voice.trunkDiscoveries.Count(e=>e.Kind=="Decoded audio"),controlAfter=trunkDiscoveries.Count(e=>e.Kind=="Decoded audio");
    if(ownAfter-ownBefore!=(expectAudio?1:0)||controlAfter-controlBefore!=(expectAudio?1:0))throw new IOException(name+": decoded-audio success was missing, duplicated or falsely logged");
    if(expectAudio){var row=voice.trunkDiscoveries.First(e=>e.Kind=="Decoded audio");if(row.Talkgroup!=tg||row.Hz!=TrunkAudioTests.Call.Hz||row.Nac!=nac||row.Label!="Synthetic dispatch"||row.Radio!=456||row.Receiver!=voice.fleetProfile.Name||row.Control!=fleetProfile.Name||!voice.LogBox.Text.Contains("Trunk Decoded audio:")||!LogBox.Text.Contains("Trunk Decoded audio:"))throw new IOException(name+": logged assignment metadata does not match actual followed audio");}
    else if(voice.trunkRadio.AudioFrames!=0)throw new IOException(name+": unexpected decoded sample count");
    passed++;
   }
   await Case("matching clear voice",TrunkAudioTests.ClearSymbols,123,0x293,true);
   await TrunkAudioTests.Feed(voice.trunkRadio,TrunkAudioTests.Terminator());voice.TickTrunk();voice.TickFleet();if(voice.fleetOwner!=null||!trunkDiscoveries.Any(e=>e.Kind=="Call end"&&e.Detail=="P25 terminator"&&e.Talkgroup==123))throw new IOException("Actual voice terminator did not release and log the receiver");
   TickFleet();if(voice.fleetOwner!=null)throw new IOException("An ended call was dispatched again from a cached grant without new control evidence");
   await Case("wrong group",TrunkAudioTests.ClearSymbols,124,0x293,false);
   await Case("wrong NAC",TrunkAudioTests.ClearSymbols,123,0x294,false);
   await Case("encrypted voice",VoiceFixtures.EncryptedHdu.Concat(VoiceFixtures.Ldu1).Concat(VoiceFixtures.EncryptedLdu2).ToArray(),123,0x293,false);
   await Case("unknown encryption",VoiceFixtures.Ldu1,123,0x293,false);
   await Case("next clear call",TrunkAudioTests.ClearSymbols,123,0x293,true);
   File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"trunk-audio-ui.txt"),$"PASS: {passed} synthetic grant/tune/voice/log cases; exactly one success per decoded clear call on both receivers. No live RF/GPS or transmission.");
  }finally{StopTrunk(false);if(voice!=null&&!voice.closing)voice.Close();fleetProfile=original;ReflectFleet();}
 }
}
