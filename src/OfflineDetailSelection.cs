using System.IO;
using System.Text.Json;
namespace MRS;

// Stores only a private file reference; never installs map data or enables networking.
internal sealed class OfflineDetailSelection : IDisposable
{
 private readonly Func<string,Task<FoxOfflineDetail>> load;
 internal OfflineDetailSelection(Func<string,Task<FoxOfflineDetail>>? loader=null){load=loader??(path=>Task.Run(()=>FoxOfflineDetail.Load(path)));}
 private readonly object sync=new();private string settings="";private int generation;private bool disposed;
 internal FoxOfflineDetail? Current{get;private set;}
 internal string SelectedPath{get;private set;}="";
 internal string Status{get;private set;}="";
 internal event Action? Changed;
 internal async Task Configure(string settingsPath)
 {
  lock(sync){if(disposed)return;settings=settingsPath;}
  if(!File.Exists(settingsPath))return;
  try{
   string path;
   // Release the preference file before any asynchronous map read. A removal or
   // replacement must be able to atomically save while restore is still loading.
   using(var stream=new FileStream(settingsPath,FileMode.Open,FileAccess.Read,FileShare.Read)){
    if(stream.Length>8192)throw new IOException("Saved map choice is too large");
    using var json=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=4});var root=json.RootElement;
    if(root.GetProperty("Version").GetInt32()!=1)throw new IOException("Unsupported saved map choice version");
    path=root.GetProperty("Path").GetString()??"";
   }
   if(path.Length==0)return;
   if(!Path.IsPathFullyQualified(path)||path.StartsWith(@"\\"))throw new IOException("Select a map on a local drive before restoring it automatically");
   await Load(path,false);
  }catch(Exception ex){lock(sync){if(disposed)return;Status="Saved offline detail unavailable: "+ex.Message;}Changed?.Invoke();}
 }
 internal async Task Load(string path,bool remember=true)
 {
  string full=Path.GetFullPath(path);int request;
  lock(sync){if(disposed)return;request=++generation;Status="Loading local offline detail…";}Changed?.Invoke();
  try{
   var loaded=await load(full);
   lock(sync){if(disposed||request!=generation)return;Current=loaded;SelectedPath=full;Status="";if(remember)Persist(full);}
   Changed?.Invoke();
  }catch(Exception ex){bool latest;lock(sync){latest=!disposed&&request==generation;if(latest)Status="Offline detail load failed: "+ex.Message+(Current!=null?" · previous map retained":"");}if(latest){Changed?.Invoke();throw;}}
 }
 internal void Remove()
 {
  lock(sync){if(disposed)return;generation++;Current=null;SelectedPath="";Status="";Persist("");}Changed?.Invoke();
 }
 private void Persist(string path)
 {
  if(settings.Length==0)return;string temporary=settings+"."+Guid.NewGuid().ToString("N")+".tmp";
  try{Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settings))!);File.WriteAllText(temporary,JsonSerializer.Serialize(new{Version=1,Path=path}));File.Move(temporary,settings,true);}
  catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){Status="Map selection active; could not save preference: "+ex.Message;}
  finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
 }
 internal string Description=>Current is {} detail?$"{detail.Name}\nSource (user supplied): {detail.Source}\nLicence: {detail.License}\nVersion: {detail.Version}\nSize: {detail.Bytes/1048576.0:0.0} MB\n{detail.Features.Count} features":"No local detail loaded";
 public void Dispose(){lock(sync){disposed=true;generation++;}Changed=null;}
}
