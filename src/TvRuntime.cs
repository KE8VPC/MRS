using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
namespace MRS;
internal static class TvRuntime
{
    internal static string Assets => Path.Combine(AppContext.BaseDirectory,"Decoders","ATSC");
    internal static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MRS","Decoders","ATSC");
    internal static string Runtime => Environment.GetEnvironmentVariable("MRS_ATSC_RUNTIME") ?? Path.Combine(Root,"runtime-v1");
    internal static string Python => Path.Combine(Runtime,"python.exe");
    internal static bool Available => File.Exists(Python) && File.Exists(Path.Combine(Assets,"atsc_worker.py"));
    internal static ProcessStartInfo Command(string executable, params string[] args)
    {
        var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        info.Environment["PATH"]=Path.Combine(Runtime,"Library","bin")+Path.PathSeparator+Runtime+Path.PathSeparator+Environment.GetEnvironmentVariable("PATH");
        info.Environment["PYTHONNOUSERSITE"]="1";
        info.Environment.Remove("PYTHONPATH");info.Environment.Remove("PYTHONHOME");
        foreach(var arg in args)info.ArgumentList.Add(arg);return info;
    }
    internal static async Task<string> Run(ProcessStartInfo info, CancellationToken cancel, Action<string>? progress=null)
    {
        using var process=new Process{StartInfo=info};process.Start();
        void Kill(){try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}}
        using var registration=cancel.Register(Kill);
        var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(cancel);string output=await stdout,error=await stderr;cancel.ThrowIfCancellationRequested();if(process.ExitCode!=0)throw new IOException((error+"\n"+output).Trim());progress?.Invoke(output.Trim());return output; }
        finally{Kill();}
    }
    internal static async Task Probe(CancellationToken token)
    {
        if(!Available)throw new IOException("Use Set up TV decoder first.");
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);timeout.CancelAfter(TimeSpan.FromSeconds(30));
        string result=await Run(Command(Python,Path.Combine(Assets,"atsc_worker.py"),"--probe"),timeout.Token);
        if(!result.Contains("\"kind\": \"ready\""))throw new IOException("TV decoder probe did not report ready.");
    }
    internal static async Task Install(CancellationToken token, Action<string> progress)
    {
        Directory.CreateDirectory(Root);
        using var installLock=new FileStream(Path.Combine(Root,"setup.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if(Available)
        {
            try{await Probe(token);progress("TV decoder is already installed and ready.");return;}
            catch(Exception)when(!token.IsCancellationRequested){progress("Existing TV runtime did not pass its probe; repairing the pinned installation...");}
        }
        string archive=Path.Combine(Root,"micromamba-2.9.0.tar.bz2"),exe=Path.Combine(Root,"Library","bin","micromamba.exe");
        progress("Downloading the pinned decoder setup tool...");
        using(var client=new HttpClient{Timeout=TimeSpan.FromMinutes(5)})
        {byte[] bytes=await client.GetByteArrayAsync("https://micro.mamba.pm/api/micromamba/win-64/2.9.0",token);if(Convert.ToHexString(SHA256.HashData(bytes))!="97A336F4AB794BD96A6A4DA5E6ED63E75A1D31830414A182419B23D3B36F3FE0")throw new IOException("TV setup tool checksum mismatch.");await File.WriteAllBytesAsync(archive,bytes,token);}
        await Run(Command(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"tar.exe"),"-xf",archive,"-C",Root,"Library/bin/micromamba.exe"),token);
        progress("Installing pinned GNU Radio and playback packages. First setup needs internet and roughly 1.3 GB for the runtime, plus download cache. This may take several minutes...");
        await Run(Command(exe,"--no-rc","create","--yes","--root-prefix",Path.Combine(Root,"packages"),"--prefix",Path.Combine(Root,"runtime-v1"),"--file",Path.Combine(Assets,"runtime-lock.txt")),token);
        await Probe(token);progress("ATSC 1.0 decoder and player are ready.");
    }
}
