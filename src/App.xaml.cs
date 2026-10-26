using System.IO;
using System.Windows;

namespace MRS;
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if(e.Args.Length==2&&e.Args[0]=="--sensor-fixture-test"){try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"sensor-fixture-test.txt"),Task.Run(()=>SensorTests.Fixtures(e.Args[1])).GetAwaiter().GetResult());Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"sensor-fixture-test.txt"),ex.ToString());Shutdown(1);}return;}
        if(e.Args.Length==2&&e.Args[0]=="--export-bandplans"){CsvBandPlans.Seed(Path.GetFullPath(e.Args[1]));Shutdown(0);return;}
        if(e.Args.Length==2&&e.Args[0]=="--trunk-flat-test")
        {
            try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"trunk-test.txt"),TrunkTests.Analyze(e.Args[1]));Shutdown(0);}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"trunk-test.txt"),ex.ToString());Shutdown(1);}return;
        }
        if(e.Args.Length==2&&e.Args[0]=="--dmr-iq-test")
        {
            try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"dmr-test.txt"),DmrTests.Iq(e.Args[1]));Shutdown(0);}catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"dmr-test.txt"),ex.ToString());Shutdown(1);}return;
        }
        if(e.Args.Length==2&&e.Args[0]=="--aviation-test")
        {
            try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"aviation-test.txt"),Task.Run(()=>AviationTests.Fixtures(e.Args[1])).GetAwaiter().GetResult());Shutdown(0);}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"aviation-test.txt"),ex.ToString());Shutdown(1);}return;
        }
        if(e.Args.Length==2 && e.Args[0] is "--paging-test" or "--paging-iq-test")
        {
            try { var report=Task.Run(()=>e.Args[0]=="--paging-test"?PagingTests.Fixtures(e.Args[1]):PagingTests.Iq(e.Args[1])).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"paging-test.txt"),report);Shutdown(0); }
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"paging-test.txt"),ex.ToString());Shutdown(1);}return;
        }
        if(e.Args.Length==3 && e.Args[0]=="--tv-stream-test")
        {
            try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tv-stream-test.txt"),Task.Run(()=>TvTests.Stream(e.Args[1],e.Args[2])).GetAwaiter().GetResult());Shutdown(0);}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tv-stream-test.txt"),ex.ToString());Shutdown(1);}
            return;
        }
        if(e.Args.Contains("--setup-tv"))
        {
            try{Task.Run(()=>TvRuntime.Install(CancellationToken.None,_=>{})).GetAwaiter().GetResult();File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tv-setup-test.txt"),"PASS: pinned runtime installed and probed.");Shutdown(0);}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"tv-setup-test.txt"),ex.ToString());Shutdown(1);}
            return;
        }
        if (e.Args.Length > 0 && e.Args[0].StartsWith("--hackrf-"))
        {
            Shutdown(HackRfNative.Run(e.Args)); return;
        }
        if(e.Args.Length==3 && e.Args[0]=="--hd-stream-test")
        {
            try{File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"hd-stream-test.txt"),Task.Run(()=>HdStreamTests.Run(e.Args[1],e.Args[2])).GetAwaiter().GetResult());Shutdown(0);}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"hd-stream-test.txt"),ex.ToString());Shutdown(1);}
            return;
        }
        if (e.Args.Contains("--rtl-fixture"))
        {
            try
            {
                ReceiverStream.Fixture(e.Args.Length > 1 ? int.Parse(e.Args[1]) : RtlNative.SampleRate, e.Args.Length > 2 ? int.Parse(e.Args[2]) : 1, e.Args.Length > 3 ? int.Parse(e.Args[3]) : -1000, e.Args.Length > 4 ? int.Parse(e.Args[4]) : 0, e.Args.Length > 5 ? uint.Parse(e.Args[5]) : 146520000);
                Shutdown(0);
            }
            catch { Shutdown(1); }
            return;
        }
        if (e.Args.Contains("--setup-rtl"))
        {
            try { Task.Run(RtlReceiver.InstallLibrary).GetAwaiter().GetResult(); Shutdown(0); }
            catch (Exception ex) { Console.Error.WriteLine(ex.Message); Shutdown(1); }
            return;
        }
        if (e.Args.Length > 0 && (e.Args[0] == "--rtl-probe" || e.Args[0] == "--rtl-worker"))
        {
            Shutdown(RtlNative.Run(e.Args));
            return;
        }
        if (e.Args.Contains("--self-test"))
        {
            try { var report = SelfTests.Run(); File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.txt"), report); Shutdown(0); }
            catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "self-test.txt"), ex.ToString()); Shutdown(1); }
            return;
        }
        var window = new MainWindow(e.Args.Contains("--ui-smoke")||e.Args.Contains("--fleet-smoke"));
        MainWindow = window;
        if (e.Args.Contains("--ui-smoke")||e.Args.Contains("--fleet-smoke"))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            string diagnosticError = Path.Combine(AppContext.BaseDirectory, "ui-smoke-error.txt");
            if (File.Exists(diagnosticError)) File.Delete(diagnosticError);
            string diagnosticResult = Path.Combine(AppContext.BaseDirectory, "ui-smoke-result.txt");
            if (File.Exists(diagnosticResult)) File.Delete(diagnosticResult);
            window.ShowInTaskbar = false;
            window.Left = -16000;
            window.Loaded += async (_, _) =>
            {
                try { if(e.Args.Contains("--fleet-smoke"))await window.RenderUpdate039Smoke(_=>{});else await window.RenderSmokeTest(Path.Combine(AppContext.BaseDirectory, "ui-smoke.png")); File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"ui-smoke-result.txt"),"PASS: all UI smoke assertions completed"); window.Close(); Shutdown(0); }
                catch (Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "ui-smoke-error.txt"), ex.ToString()); window.Close(); Shutdown(1); }
            };
        }
        window.Show();
    }
}

