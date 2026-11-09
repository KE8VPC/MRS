using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace MRS;
public partial class MainWindow
{
 internal async Task RenderDmrFollowingSmoke()
 {
  Stop();var original=fleetProfile;MainWindow? voice=null;int passed=0;
  async Task Wait(Func<bool> ready){for(int n=0;n<700&&!ready();n++)await Task.Delay(10);if(!ready())throw new IOException("Synthetic DMR assignment timed out");}
  try{
   fleetProfile=new(workspaceSlot,"Synthetic DMR control","Trunk control","DMR test pool");ReflectFleet();
   voice=new MainWindow(true,"B",Path.Combine(dataFolder,"dmr-voice")){ShowInTaskbar=false,Left=-16000};voice.fleetProfile=new("B","Synthetic DMR voice","Trunk voice","DMR test pool");voice.ReflectFleet();voice.Show();voice.rfMode=true;voice.engine.StartExternal(2);voice.receiver.Start(0,461000000,0,false,fixture:true,sampleRate:240000,fixtureSeconds:40);await Wait(()=>voice.receiver.AppliedFrequency>0);voice.receiver.RawIq-=voice.dmrTrunkRadio.Feed;
   foreach(string protocol in new[]{"Capacity Plus","Connect Plus","DMR Tier III"}){
    StopDmrTrunk(false);var profile=DmrFollowingTests.Profile(protocol);dmrTrunkId=Guid.NewGuid().ToString("N");dmrTrunkName.Text="Synthetic "+protocol;dmrTrunkMode.SelectedItem=protocol;dmrTrunkControl.Text="461.000000";dmrTrunkMaps.Clear();foreach(var carrier in profile.Carriers)dmrTrunkMaps.Add(new(){Lcn=carrier.Lcn,FrequencyMHz=PlanCsv.MHz(carrier.Hz),Confirmed=true,ColorCode=carrier.ColorCode,Evidence="Synthetic map fixture"});SaveDmrTrunkProfile();dmrTrunkProfiles.SelectedItem=dmrTrunkSaved[dmrTrunkId].Profile;profile=ReadDmrTrunkProfile();if(profile.Carriers.Any(c=>c.ColorCode!=6))throw new IOException("Map editor/save/reload lost color code");
    dmrTrunkRadio.Start(profile);dmrTrunkRadio.SuppressLocalVoice=true;lock(dmrTrunkRadio.Sync)dmrTrunkRadio.Model.HangSeconds=15;
    int group=protocol=="Capacity Plus"?91:protocol=="Connect Plus"?1216:258;
    void Observe(){var now=DateTimeOffset.UtcNow;lock(dmrTrunkRadio.Sync){if(protocol=="Capacity Plus")dmrTrunkRadio.Model.ObserveCap(new(1,[new(4,group)]),profile.ControlHz,now);else dmrTrunkRadio.Model.ObserveDmr(protocol=="Connect Plus"?DmrFollowingTests.ConGrant:DmrFollowingTests.TierGrant,profile.ControlHz,now);}}
    Observe();TickDmrFleet();if(voice.dmrFleetOwner!=null)throw new IOException(protocol+": single observation bypassed normal repeat confirmation");Observe();TickDmrFleet();await Wait(()=>voice.dmrFleetOwner==this&&!voice.dmrFleetVoiceBusy&&voice.dmrTrunkRadio.Running);
    voice.TickDmrTrunk();voice.dmrTrunkUiAt=DateTimeOffset.MinValue;voice.TickDmrTrunk();var row=voice.dmrTrunkDiscoveries.First(e=>e.Kind=="Tuned"&&e.Protocol==protocol);
    long expected=protocol=="DMR Tier III"?461200000:461150000;if(row.Talkgroup!=group||row.Hz!=expected||row.Slot!=2||row.ColorCode!=6||!voice.DmrFollowingCallSummary.Text.Contains("slot 2")||!voice.DmrFollowingCallSummary.Text.Contains("CC 6")||!voice.LogBox.Text.Contains("slot 2")||!LogBox.Text.Contains("CC 6"))throw new IOException(protocol+": assignment/history/header missing mapped frequency, slot or color code");
    if(voice.dmrTrunkSetup?.Visibility!=Visibility.Collapsed||voice.dmrTrunkVoiceActions?.Visibility!=Visibility.Visible)throw new IOException("Dedicated voice window still exposes the unrelated profile editor");
    if(voice.dmrTrunkDiscoveries.Any(e=>e.Kind=="Decoded audio"))throw new IOException("A tune-only DMR assignment falsely claimed decoded voice");
    voice.SelectWorkspace(voice.dmrTrunkFollowingTab!);voice.UpdateLayout();var bitmap=new RenderTargetBitmap((int)voice.ActualWidth,(int)voice.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(voice);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-dmr-follow-"+passed+".png")))png.Save(file);
    lock(voice.dmrTrunkRadio.Sync)voice.dmrTrunkRadio.Model.End(DateTimeOffset.UtcNow,"Synthetic DMR lease end");voice.TickDmrTrunk();voice.TickDmrFleet();TickDmrFleet();if(voice.dmrFleetOwner!=null||!dmrTrunkDiscoveries.Any(e=>e.Kind=="Call end"&&e.Protocol==protocol&&e.Slot==2&&e.ColorCode==6))throw new IOException(protocol+": released slot was lost or redispatched from cached data");
    Observe();TickDmrFleet();await Wait(()=>voice.dmrFleetOwner==this&&!voice.dmrFleetVoiceBusy);
    var pause=voice.dmrTrunkVoiceActions!.Children.OfType<Button>().Single(b=>b.Content?.ToString()=="Pause voice receiver");pause.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));TickDmrFleet();if(!voice.fleetPaused||voice.dmrFleetOwner!=null)throw new IOException("Pause did not release and withhold the dedicated receiver");
    var resume=voice.dmrTrunkVoiceActions.Children.OfType<Button>().Single(b=>b.Content?.ToString()=="Resume voice pool");resume.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));TickDmrFleet();await Wait(()=>voice.dmrFleetOwner==this&&!voice.dmrFleetVoiceBusy);if(voice.fleetPaused)throw new IOException("Resume left the voice receiver paused");
    voice.dmrFleetAssigned=DateTimeOffset.UtcNow.AddSeconds(-dmrTrunkRadio.Model.MaximumSeconds-1);TickDmrFleet();if(voice.dmrFleetOwner!=null)throw new IOException("Maximum timer immediately redispatched the retired cached grant in the same tick");
    Observe();TickDmrFleet();await Wait(()=>voice.dmrFleetOwner==this&&!voice.dmrFleetVoiceBusy);
    dmrTrunkIgnore.Text=group.ToString();ApplyDmrTrunkRules();TickDmrFleet();if(voice.dmrFleetOwner!=null||!voice.dmrTrunkDiscoveries.Any(e=>e.Kind=="Released"&&e.Detail.Contains("filter")))throw new IOException("Changed control filters did not release an already assigned voice call");
    dmrTrunkIgnore.Clear();ApplyDmrTrunkRules();Observe();TickDmrFleet();await Wait(()=>voice.dmrFleetOwner==this&&!voice.dmrFleetVoiceBusy);passed++;
   }
   File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"dmr-follow-ui.txt"),$"PASS: {passed} synthetic mapped DMR protocol profiles; save/reload color codes, repeated-grant dispatch, slot/CC history, release, fresh-grant renewal and dedicated pause/resume controls. No AMBE/RF voice acceptance.");
  }finally{StopDmrTrunk(false);if(voice!=null&&!voice.closing)voice.Close();fleetProfile=original;ReflectFleet();}
 }
}
