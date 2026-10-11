using System.IO;
namespace MRS;
internal static class SharedFrequencyTests
{
    internal static void Run(Action<bool,string> check)
    {
        string root=Path.Combine(Path.GetTempPath(),"mrs-shared-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string path=Path.Combine(root,"frequencies.json"), legacyPath=Path.Combine(root,"b.json");
            var a=new FrequencyLibrary(path);
            var signal=new FrequencyItem {Name="A signal",FrequencyHz=146520000}; a.Upsert(signal);
            var b=new FrequencyLibrary(legacyPath);
            var folder=new FrequencyItem {Name="B folder",IsFolder=true}; b.Upsert(folder);
            b.Upsert(new FrequencyItem {Name="B signal",FrequencyHz=147000000,ParentId=folder.Id});
            string original=File.ReadAllText(legacyPath);
            a.ImportReceiverB(legacyPath);
            check(a.Items.Count==4 && a.Items.Contains(signal),"Shared migration preserves A and imports B hierarchy");
            check(File.ReadAllText(legacyPath)==original,"Shared migration leaves legacy B file unchanged");
            var reopened=new FrequencyLibrary(path); reopened.ImportReceiverB(legacyPath);
            check(reopened.Items.Count==4,"Receiver B import is persistent and runs only once");
            var panelA=new FrequencyPanel(); var panelB=new FrequencyPanel();
            try
            {
                panelA.Initialize(path); panelB.Initialize(path);
                int updates=0; panelB.Changed+=()=>updates++;
                reopened.Upsert(signal with {Name="Updated A"});
                check(panelA.Markers.Any(x=>x.Name=="Updated A") && panelB.Markers.Any(x=>x.Name=="Updated A") && updates>0,"Shared save refreshes markers in both panels");
                bool rejected=false; try{a.Upsert(signal with {Name="Stale"});}catch(IOException){rejected=true;}
                check(rejected && new FrequencyLibrary(path).Items.Any(x=>x.Name=="Updated A"),"Stale library cannot overwrite another window's saved edit");
                reopened.Delete(signal.Id);
                check(!panelA.Markers.Any(x=>x.Id==signal.Id)&&!panelB.Markers.Any(x=>x.Id==signal.Id),"Shared delete refreshes both panels");
                reopened.Undo();
                check(panelA.Markers.Any(x=>x.Id==signal.Id)&&panelB.Markers.Any(x=>x.Id==signal.Id),"Shared undo refreshes both panels");
            }
            finally{panelA.Release();panelB.Release();}
            string bad=Path.Combine(root,"bad.json"); File.WriteAllText(bad,"broken");
            string before=File.ReadAllText(path);
            try {new FrequencyLibrary(Path.Combine(root,"new.json")).ImportReceiverB(bad);}catch(System.Text.Json.JsonException){}
            check(File.ReadAllText(path)==before && File.ReadAllText(bad)=="broken","Malformed legacy input does not rewrite original files");
        }
        finally { Directory.Delete(root,true); }
    }
}
