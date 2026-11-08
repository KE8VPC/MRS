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
   voice=new MainWindow(true,"B",Path.Combine(dataFolder,"dmr-voice")){ShowInTaskbar=false,Left=-16000};voice.fleetProfile=new("B","Synthetic DMR voice","Trunk voice","DMR test pool");voice.ReflectFleet();voice.Show();voice.rfMode=true;voice.engine.StartExternal(2);voice.receiver.Start(0,461000000,0,false,fixture:true,sampleRate:240000,fixtureSeconds:40);await Wait(()=>voice.receiver.AppliedFrequency>0);voice.receiver.RawIq-=voice.trunkRadio.Feed;
   foreach(string protocol in new[]{"Capacity Plus","Connect Plus","DMR Tier III"}){
    StopTrunk(false);var profile=DmrFollowingTests.Profile(protocol);trunkId=Guid.NewGuid().ToString("N");trunkName.Text="Synthetic "+protocol;trunkMode.SelectedItem=protocol;trunkControl.Text="461.000000";trunkMaps.Clear();foreach(var carrier in profile.Carriers)trunkMaps.Add(new(){Lcn=carrier.Lcn,FrequencyMHz=PlanCsv.MHz(carrier.Hz),Confirmed=true,ColorCode=carrier.ColorCode,Evidence="Synthetic map fixture"});SaveTrunkProfile();trunkProfiles.SelectedItem=trunkSaved[trunkId].Profile;profile=ReadTrunkProfile();if(profile.Carriers.Any(c=>c.ColorCode!=6))throw new IOException("Map editor/save/reload lost color code");
    trunkRadio.Start(profile);trunkRadio.SuppressLocalVoice=true;lock(trunkRadio.Sync)trunkRadio.Model.HangSeconds=15;
    int group=protocol=="Capacity Plus"?91:protocol=="Connect Plus"?1216:258;
    void Observe(){var now=DateTimeOffset.UtcNow;lock(trunkRadio.Sync){if(protocol=="Capacity Plus")trunkRadio.Model.ObserveCap(new(1,[new(4,group)]),profile.ControlHz,now);else trunkRadio.Model.ObserveDmr(protocol=="Connect Plus"?DmrFollowingTests.ConGrant:DmrFollowingTests.TierGrant,profile.ControlHz,now);}}
    Observe();TickFleet();if(voice.fleetOwner!=null)throw new IOException(protocol+": single observation bypassed normal repeat confirmation");Observe();TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy&&voice.trunkRadio.Running);
    voice.TickTrunk();voice.trunkUiAt=DateTimeOffset.MinValue;voice.TickTrunk();var row=voice.trunkDiscoveries.First(e=>e.Kind=="Tuned"&&e.Protocol==protocol);
    long expected=protocol=="DMR Tier III"?461200000:461150000;if(row.Talkgroup!=group||row.Hz!=expected||row.Slot!=2||row.ColorCode!=6||!voice.FollowingCallSummary.Text.Contains("slot 2")||!voice.FollowingCallSummary.Text.Contains("CC 6")||!voice.LogBox.Text.Contains("slot 2")||!LogBox.Text.Contains("CC 6"))throw new IOException(protocol+": assignment/history/header missing mapped frequency, slot or color code");
    if(voice.trunkSetup?.Visibility!=Visibility.Collapsed||voice.trunkVoiceActions?.Visibility!=Visibility.Visible)throw new IOException("Dedicated voice window still exposes the unrelated profile editor");
    if(voice.trunkDiscoveries.Any(e=>e.Kind=="Decoded audio"))throw new IOException("A tune-only DMR assignment falsely claimed decoded voice");
    voice.SelectWorkspace(voice.trunkFollowingTab!);voice.UpdateLayout();var bitmap=new RenderTargetBitmap((int)voice.ActualWidth,(int)voice.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(voice);var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-dmr-follow-"+passed+".png")))png.Save(file);
    lock(voice.trunkRadio.Sync)voice.trunkRadio.Model.End(DateTimeOffset.UtcNow,"Synthetic DMR lease end");voice.TickTrunk();voice.TickFleet();TickFleet();if(voice.fleetOwner!=null||!trunkDiscoveries.Any(e=>e.Kind=="Call end"&&e.Protocol==protocol&&e.Slot==2&&e.ColorCode==6))throw new IOException(protocol+": released slot was lost or redispatched from cached data");
    Observe();TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy);
    var pause=voice.trunkVoiceActions!.Children.OfType<Button>().Single(b=>b.Content?.ToString()=="Pause voice receiver");pause.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));TickFleet();if(!voice.fleetPaused||voice.fleetOwner!=null)throw new IOException("Pause did not release and withhold the dedicated receiver");
    var resume=voice.trunkVoiceActions.Children.OfType<Button>().Single(b=>b.Content?.ToString()=="Resume voice pool");resume.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy);if(voice.fleetPaused)throw new IOException("Resume left the voice receiver paused");
    voice.fleetAssigned=DateTimeOffset.UtcNow.AddSeconds(-trunkRadio.Model.MaximumSeconds-1);TickFleet();if(voice.fleetOwner!=null)throw new IOException("Maximum timer immediately redispatched the retired cached grant in the same tick");
    Observe();TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy);
    trunkIgnore.Text=group.ToString();ApplyTrunkRules();TickFleet();if(voice.fleetOwner!=null||!voice.trunkDiscoveries.Any(e=>e.Kind=="Released"&&e.Detail.Contains("filter")))throw new IOException("Changed control filters did not release an already assigned voice call");
    trunkIgnore.Clear();ApplyTrunkRules();Observe();TickFleet();await Wait(()=>voice.fleetOwner==this&&!voice.fleetVoiceBusy);passed++;
   }
   File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"dmr-follow-ui.txt"),$"PASS: {passed} synthetic mapped DMR protocol profiles; save/reload color codes, repeated-grant dispatch, slot/CC history, release, fresh-grant renewal and dedicated pause/resume controls. No AMBE/RF voice acceptance.");
  }finally{StopTrunk(false);if(voice!=null&&!voice.closing)voice.Close();fleetProfile=original;ReflectFleet();}
 }
}
