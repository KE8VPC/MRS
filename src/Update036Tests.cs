using System.IO;
namespace MRS;
internal static class Update036Tests
{
    internal static void Run(Action<bool,string> check)
    {
        var parser=new NxdnControlParser();
        check(parser.Feed("Location Information - Cat: Local - Sys Code: 289 - Site Code 12")==null,"NXDN location without a valid control header cannot create an identity");
        parser.Feed("Sync: NXDN48 RCCH Data RAN 12 CAC SRV_INFO");parser.Feed("Service Information - Location ID [40242C] SVC [BFC0]");var id=parser.Feed("Location Information - Cat: Local - Sys Code: 289 - Site Code 12");
        check(id==new NxdnIdentity(289,12,12,"40242C"),"NXDN valid service announcement retains system/site/RAN/location");
        check(parser.Feed("Location Information - Cat: Local - Sys Code: 999 - Site Code 99")==null,"NXDN parser consumes one identity per valid control frame");
        parser.Feed("Sync: NXDN48 RCCH Data RAN 12 CAC SRV_INFO (CRC ERR)");parser.Feed("Service Information - Location ID [40242C]");check(parser.Feed("Location Information - Cat: Local - Sys Code: 289 - Site Code 12")==null,"NXDN CRC failures cannot populate identities");
        parser.Feed("Sync: NXDN48 RCCH Data RAN 12 CAC SITE_INFO");parser.Reset();parser.Feed("Location ID [40242C]");check(parser.Feed("Location Information - Cat: Local - Sys Code: 289 - Site Code 12")==null,"NXDN partial control state cannot cross a reset");
        check(BroadcastChannels.Am.Length==118&&BroadcastChannels.Am[0].Hz==530000&&BroadcastChannels.Am[1].Hz==540000&&BroadcastChannels.Am[^1].Hz==1700000,"AM channel centers distinguish supplemental 530 from 117 broadcast channels");
        check(BroadcastChannels.Fm.Length==100&&BroadcastChannels.Fm[0].Hz==88100000&&BroadcastChannels.Fm[^1].Hz==107900000&&BroadcastChannels.Fm[^1].Label.Contains("300"),"FM channels 201–300 retain exact 200 kHz centers");
        string root=Path.Combine(AppContext.BaseDirectory,"update036-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);var library=new FrequencyLibrary(Path.Combine(root,"freq.json"));var original=new FrequencyItem{Name="My channel",FrequencyHz=461637500,Notes="Keep notes",Signaling="User annotation",Protocols="Other"};library.Upsert(original);var reception=new NxdnReception(id!,new("rf-test","Test",false,461637500),DateTimeOffset.UtcNow);library.SaveNxdn(reception,"A");library.SaveNxdn(reception,"A");var saved=library.Items.Single(i=>!i.IsFolder);
        check(saved.Name==original.Name&&saved.Notes==original.Notes&&saved.Signaling.Contains("User annotation")&&saved.Signaling.Split("NXDN").Length==2&&saved.Protocols=="Other; NXDN","NXDN save annotates idempotently while preserving user labels and notes");
        library.SaveNxdn(reception with{Context=new("replay-test","Replay",true,462000000)},"A");check(library.Items.Count(i=>!i.IsFolder)==1,"Replay NXDN observation cannot create a live saved frequency");
        var topics=HelpTopics.Load(Path.Combine(AppContext.BaseDirectory,"Help"));check(topics.Count>=20,"Offline help topics are available beside executable");check(HelpTopics.Search(topics,"MPX pilot").Any(t=>t.Title.Contains("FM broadcast")),"Help search matches multiple functional keywords");check(!HelpTopics.Search(topics,"not-a-real-function-xyz").Any(),"Help search does not invent matches");
        BroadcastChannels.Seed(root);var plans=new CsvBandPlans(root);plans.Reload();check(plans.Markers.Count==218&&plans.Error=="","Broadcast CSV seed contains 218 channel centers");string am=Path.Combine(root,"reference/broadcast-am/channels.csv");string text=File.ReadAllText(am);File.WriteAllText(am,text.Replace("540 kHz","My label"));BroadcastChannels.Seed(root);check(File.ReadAllText(am).Contains("My label"),"Broadcast reference seeding preserves edited CSVs");
        string custom=BandPlanFiles.CustomPath(root,"Test plan");PlanCsv.Write(Path.Combine(custom,"channels.csv"),[new(){["downlink_mhz"]="461.6375",["label"]="Site, twelve",["color"]="#22AABB",["extra"]="kept"}],[]);BandPlanFiles.Validate(custom);var c=new CsvBandPlans(custom);c.Reload();check(c.Markers.Single().Color=="#22AABB"&&PlanCsv.Read(Path.Combine(custom,"channels.csv"))[0]["extra"]=="kept","Custom plan validation retains quoted labels, colors and extra metadata");
        PlanCsv.Write(Path.Combine(custom,"channels.csv"),[new(){["downlink_mhz"]="bad"}],[]);bool rejected=false;try{BandPlanFiles.Validate(custom);}catch(IOException){rejected=true;}check(rejected,"Invalid imported frequency fails validation before save");
    }
}
public partial class MainWindow
{
    private void RenderUpdate036Smoke(Action<string> capture)
    {
        SidebarTabs.SelectedItem=AudioTab;UpdateLayout();capture("ui-audio-tab.png");
        if(SidebarTabs.Items.IndexOf(AudioTab)!=4)throw new InvalidOperationException("Audio sidebar order");
        SelectWorkspace(AmTab);UpdateLayout();if(SharedSpectrum.Parent!=AmSpectrumHost)throw new InvalidOperationException("AM spectrum host");capture("ui-am-broadcast.png");
        SelectWorkspace(FmTab);UpdateLayout();if(SharedSpectrum.Parent!=FmSpectrumHost)throw new InvalidOperationException("FM spectrum host");capture("ui-fm-workspace.png");SelectWorkspace(SpectrumTab);UpdateLayout();if(SpectrumTab.Content!=SharedSpectrum)throw new InvalidOperationException("Spectrum restore");
        ShowBandManager();UpdateLayout();capture("ui-bandplan-manager.png");
        void WindowCapture(System.Windows.Window window,string name){window.ShowInTaskbar=false;window.WindowStartupLocation=System.Windows.WindowStartupLocation.Manual;window.Left=-16000;window.Show();window.UpdateLayout();var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(window);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));using var stream=File.Create(Path.Combine(AppContext.BaseDirectory,name));encoder.Save(stream);}
        var help=new HelpWindow(Path.Combine(AppContext.BaseDirectory,"Help"),"FM"){Owner=this};help.SearchFor("MPX pilot");if(!help.SelectedTitle.Contains("FM broadcast"))throw new InvalidOperationException("Help function search");WindowCapture(help,"ui-searchable-help.png");help.Close();
        string target=Path.Combine(dataFolder,"bandplans/custom/Smoke editor");bool saved=false;var edit=new BandPlanEditor(target,new(){["channels.csv"]=[new(){["downlink_mhz"]="461.6375",["label"]="Example NXDN site",["color"]="#22AABB"}]},()=>saved=true){Owner=this};WindowCapture(edit,"ui-bandplan-editor.png");edit.Save();if(!saved||PlanCsv.Hz(PlanCsv.Read(Path.Combine(target,"channels.csv"))[0],"downlink")!=461637500)throw new InvalidOperationException("Bandplan editor save");

        var context=new PacketContext("nx-smoke","Synthetic NXDN",true,461637500);var t=DateTimeOffset.UtcNow;for(int i=0;i<3;i++)ObserveNxdn(new(new(289,12,12,"40242C"),context,t.AddMilliseconds(i*80)));SelectWorkspace(DmrTab);UpdateLayout();capture("ui-nxdn-identities.png");if(nxdnRows.Single(r=>r.Last.Context.Key=="nx-smoke").Count!=3)throw new InvalidOperationException("NXDN identity repeats");
    }
}
