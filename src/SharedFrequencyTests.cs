using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
            var signal=new FrequencyItem {Name="A signal",FrequencyHz=146520000,Mode="NFM",BandwidthHz=12500}; a.Upsert(signal);
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
                var requestsA=new List<FrequencyItem>();int requestsB=0;
                panelA.TuneRequested+=requestsA.Add;panelB.TuneRequested+=_=>requestsB++;
                panelA.SelectEntry(signal.Id);panelB.SelectEntry(signal.Id);
                check(requestsA.Count==0&&requestsB==0,"Programmatic saved-marker selection does not tune either receiver");
                var tree=((DockPanel)panelA.Content).Children.OfType<TreeView>().Single();
                TreeViewItem? Find(ItemsControl parent,Guid id)
                {
                    foreach(var row in parent.Items.OfType<TreeViewItem>())
                    {if(row.Tag is FrequencyItem item&&item.Id==id)return row;if(Find(row,id) is {} found)return found;}
                    return null;
                }
                var target=Find(tree,signal.Id)??throw new InvalidOperationException("Saved frequency tree row missing");
                tree.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonDownEvent,Source=target});
                tree.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,Environment.TickCount,MouseButton.Left){RoutedEvent=UIElement.PreviewMouseLeftButtonUpEvent,Source=target});
                check(requestsA.Count==1&&requestsA[0].FrequencyHz==146520000&&requestsA[0].Mode=="NFM"&&requestsA[0].BandwidthHz==12500&&requestsB==0,"Saved signal click sends its frequency, mode and filter only to its own receiver");
                panelA.ActivateClick(signal,signal);
                check(requestsA.Count==2,"Clicking the same saved signal again requests tuning again");
                panelA.ActivateClick(folder,folder);panelA.ActivateClick(null,signal);panelA.ActivateClick(signal,folder);panelA.ActivateClick(signal,null);
                check(requestsA.Count==2&&requestsB==0,"Folder clicks, cancelled drags and mismatched release targets do not tune");
                int updates=0; panelB.Changed+=()=>updates++;
                reopened.Upsert(signal with {Name="Updated A"});
                check(panelA.Markers.Any(x=>x.Name=="Updated A") && panelB.Markers.Any(x=>x.Name=="Updated A") && updates>0,"Shared save refreshes markers in both panels");
                check(requestsA.Count==2&&requestsB==0,"Shared-library refresh does not unexpectedly retune a receiver");
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
