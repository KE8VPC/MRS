using System.Diagnostics;
using System.IO;
namespace MRS;

internal static class HdStreamDecoder
{
    internal static async Task Run(string executable,int program,Func<Stream,CancellationToken,Task> feed,
        Action<byte[],int> audio,Action<string> report,CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();
        using var lifetime=CancellationTokenSource.CreateLinkedTokenSource(cancel);
        var token=lifetime.Token;
        var info=new ProcessStartInfo(executable){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(executable)!};
        foreach(string arg in new[]{"-r","-","-o","-","-t","raw","-l","2",program.ToString()})info.ArgumentList.Add(arg);
        using var process=new Process{StartInfo=info};
        process.Start();
        void Kill(){try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}}
        using var registration=token.Register(Kill);
        async Task ReadLog(){while(await process.StandardError.ReadLineAsync(token) is {} line)report(line);}
        async Task ReadAudio()
        {
            var buffer=new byte[16384];int pending=0;
            while(true)
            {
                int n=await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(pending,buffer.Length-pending),token);
                if(n==0)break;
                int total=pending+n,count=total-total%4;
                if(count>0)audio(buffer,count);
                pending=total-count;if(pending>0)Buffer.BlockCopy(buffer,count,buffer,0,pending);
            }
            if(pending!=0)throw new IOException("Incomplete stereo PCM from HD decoder.");
        }
        bool inputComplete=false;
        async Task WriteInput(){try{await feed(process.StandardInput.BaseStream,token);inputComplete=true;}finally{process.StandardInput.Close();}}
        async Task Guard(Func<Task> action){try{await action();}catch{lifetime.Cancel();throw;}}
        async Task WatchExit(){await process.WaitForExitAsync();if(!inputComplete && !token.IsCancellationRequested)throw new IOException("HD decoder ended before the I/Q source completed.");}
        try
        {
            await Task.WhenAll(Guard(ReadLog),Guard(ReadAudio),Guard(WriteInput),Guard(WatchExit));
            await process.WaitForExitAsync(cancel);cancel.ThrowIfCancellationRequested();
            if(process.ExitCode!=0)throw new IOException($"HD decoder exited with code {process.ExitCode}.");
        }
        finally{Kill();await process.WaitForExitAsync();}
    }
    internal static async Task FeedRecording(string path,Stream destination,CancellationToken cancel)
    {
        using var reader=new IqReader(path);IqFrame? first=null;HdIqConverter? converter=null;long samples=0,previous=0;
        var clock=Stopwatch.StartNew();
        while(reader.Read() is {} frame)
        {
            cancel.ThrowIfCancellationRequested();first??=frame;
            if(frame.Rate!=first.Rate || frame.Frequency!=first.Frequency || frame.Format!=IqSampleFormat.Unsigned8 || frame.Capture.UsableBandwidthHz<600000 || frame.Channel is {} channel && channel.Frequency!=frame.Frequency)
                throw new IOException("HD playback requires centered, fixed-frequency unsigned-8 I/Q with at least 600 kHz coverage.");
            if(samples>0 && (frame.ElapsedTicks-previous)/1e7>.25)throw new IOException("Recording has a reception gap; HD playback stopped.");
            converter??=new(frame.Rate);
            double delay=samples/(double)frame.Rate-clock.Elapsed.TotalSeconds;
            if(delay>0)await Task.Delay(TimeSpan.FromSeconds(delay),cancel);
            await destination.WriteAsync(converter.Process(frame.Data),cancel);
            samples+=frame.Data.Length/2;previous=frame.ElapsedTicks;
        }
        if(first==null)throw new IOException("Recording has no I/Q samples.");
    }
}
