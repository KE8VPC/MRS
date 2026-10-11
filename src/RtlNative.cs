using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace MRS;
internal static class RtlNative
{
    internal const int SampleRate = 240000;
    internal record Device(uint Index, string Name, string Manufacturer = "", string Product = "", string Serial = "", string Backend = "RTL")
    {
        public string DisplayName=>$"#{Index}: {Name} · S/N {(string.IsNullOrWhiteSpace(Serial)?"unavailable":Serial)}";
    }
    internal static string LibraryFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MRS", "rtl-v1.3.6-x64");
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibraryEx(string name, IntPtr file, uint flags);
    static RtlNative()
    {
        NativeLibrary.SetDllImportResolver(typeof(RtlNative).Assembly, (name, assembly, search) =>
        {
            if (name != "rtlsdr") return IntPtr.Zero;
            var handle = LoadLibraryEx(Path.Combine(LibraryFolder, "rtlsdr.dll"), IntPtr.Zero, 0x1100);
            if (handle == IntPtr.Zero) throw new DllNotFoundException("RTL-SDR library unavailable. Use Set up RTL library. Windows loader error " + Marshal.GetLastWin32Error());
            return handle;
        });
    }
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern uint rtlsdr_get_device_count();
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr rtlsdr_get_device_name(uint index);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_get_device_usb_strings(uint index, StringBuilder manufacturer, StringBuilder product, StringBuilder serial);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_open(out IntPtr device, uint index);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_get_usb_strings(IntPtr device, StringBuilder manufacturer, StringBuilder product, StringBuilder serial);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_close(IntPtr device);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_set_center_freq(IntPtr device, uint frequency);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_set_sample_rate(IntPtr device, uint rate);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern uint rtlsdr_get_sample_rate(IntPtr device);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_set_tuner_gain_mode(IntPtr device, int manual);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_get_tuner_gains(IntPtr device, [Out] int[]? gains);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_set_tuner_gain(IntPtr device, int gain);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_reset_buffer(IntPtr device);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_read_sync(IntPtr device, byte[] bytes, int length, out int read);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_set_direct_sampling(IntPtr device, int mode);
    [DllImport("rtlsdr", CallingConvention = CallingConvention.Cdecl)] private static extern int rtlsdr_get_direct_sampling(IntPtr device);
    private static void Check(int result, string action) { if (result < 0) throw new IOException($"{action} failed ({result}). Check the WinUSB driver and close other SDR programs."); }
    private static int SetGain(IntPtr device, int gain)
    {
        Check(rtlsdr_set_tuner_gain_mode(device, gain == -1000 ? 0 : 1), "Set gain mode");
        if (gain == -1000) return gain;
        int count = rtlsdr_get_tuner_gains(device, null);
        if (count <= 0 || count > 1024) throw new IOException("Receiver did not report supported gains.");
        int[] gains = new int[count]; Check(rtlsdr_get_tuner_gains(device, gains), "Read gains");
        int nearest = gains.MinBy(value => Math.Abs(value - gain));
        Check(rtlsdr_set_tuner_gain(device, nearest), "Set gain"); return nearest;
    }
    // Only the isolated worker invokes native APIs; a blocked USB call cannot block the desktop.
    internal static int Run(string[] args)
    {
        IntPtr device = IntPtr.Zero;
        try
        {
            if (args[0] == "--rtl-probe")
            {
                var devices = new List<Device>();
                uint count = rtlsdr_get_device_count();
                for (uint i = 0; i < count; i++)
                {
                    var maker = new StringBuilder(256); var product = new StringBuilder(256); var serial = new StringBuilder(256);
                    int result = rtlsdr_get_device_usb_strings(i, maker, product, serial);
                    devices.Add(new Device(i, $"#{i} {Marshal.PtrToStringAnsi(rtlsdr_get_device_name(i))} · " + (result == 0 ? serial.ToString() : "USB access unavailable"), result == 0 ? maker.ToString() : "", result == 0 ? product.ToString() : "", result == 0 ? serial.ToString() : ""));
                }
                Console.Write(JsonSerializer.Serialize(devices));
                return 0;
            }
            uint index = uint.Parse(args[1]); uint frequency = uint.Parse(args[2]); int gain = int.Parse(args[3]);
            int rate = args.Length > 4 ? int.Parse(args[4]) : SampleRate;
            if (!ReceiverStream.Rates.Contains(rate)) throw new IOException("Unsupported sample rate selection.");
            int samplingMode = args.Length > 6 ? int.Parse(args[6]) : 0;
            if (!SamplingModes.CanTune(frequency, samplingMode)) throw new ArgumentOutOfRangeException(nameof(frequency));
            Check(rtlsdr_open(out device, index), "Open receiver");
            if (args.Length > 5 && args[5].Length > 0)
            {
                string expected = Encoding.UTF8.GetString(Convert.FromBase64String(args[5]));
                var manufacturer = new StringBuilder(256); var product = new StringBuilder(256); var serial = new StringBuilder(256);
                Check(rtlsdr_get_usb_strings(device, manufacturer, product, serial), "Verify opened receiver identity");
                if (ReceiverIdentity.Key(new Device(index, "", manufacturer.ToString(), product.ToString(), serial.ToString())) != expected)
                    throw new IOException("Opened receiver identity changed. Find and select the intended receiver again.");
            }
            if (samplingMode != 0) Check(rtlsdr_set_direct_sampling(device, samplingMode), "Set sampling mode");
            Check(rtlsdr_set_sample_rate(device, (uint)rate), "Set sample rate");
            uint appliedRate = rtlsdr_get_sample_rate(device);
            if (appliedRate != rate) throw new IOException($"Requested {rate} S/s but receiver applied {appliedRate} S/s. This rate cannot be processed accurately.");
            Check(rtlsdr_set_center_freq(device, frequency), "Tune receiver");
            // Disabling direct sampling retunes the previous center internally; establish a valid tuner frequency first.
            if (samplingMode == 0) Check(rtlsdr_set_direct_sampling(device, 0), "Select tuner sampling");
            if (rtlsdr_get_direct_sampling(device) != samplingMode) throw new IOException("Receiver did not apply the selected sampling branch.");
            if (samplingMode != 0) gain = -1000;
            int appliedGain = samplingMode == 0 ? SetGain(device, gain) : -1000;
            Check(rtlsdr_reset_buffer(device), "Reset receiver buffer");
            using var output = Console.OpenStandardOutput();
            byte[] bytes = new byte[32768];
            var commands = new ReceiverStream.Commands(samplingMode: samplingMode);
            while (true)
            {
                uint next = commands.Take();
                int nextRate = commands.TakeRate(), nextGain = commands.TakeGain();
                if (samplingMode != 0) nextGain = int.MinValue;
                bool controlsChanged = nextRate != 0 || nextGain != int.MinValue;
                if (nextRate != 0) { Check(rtlsdr_set_sample_rate(device, (uint)nextRate), "Live sample rate"); if (rtlsdr_get_sample_rate(device) != nextRate) throw new IOException("Receiver rejected requested sample rate."); rate = nextRate; }
                if (nextGain != int.MinValue) { appliedGain = SetGain(device, nextGain); gain = nextGain; }
                if (controlsChanged || next != 0 && next != frequency)
                {
                    if (next != 0) Check(rtlsdr_set_center_freq(device, next), "Live retune");
                    Check(rtlsdr_reset_buffer(device), "Flush retune buffer");
                    int discarded = 0;
                    while (discarded < rate / 10)
                    {
                        Check(rtlsdr_read_sync(device, bytes, bytes.Length, out int settling), "Discard tuning transition");
                        if (settling <= 0) throw new IOException("No data during retune.");
                        discarded += settling;
                    }
                    if (next != 0) frequency = next;
                }
                Check(rtlsdr_read_sync(device, bytes, bytes.Length, out int count), "Read receiver");
                if (count <= 0 || count % 2 != 0) throw new IOException("Receiver returned invalid I/Q data.");
                if (rtlsdr_get_direct_sampling(device) != samplingMode) throw new IOException("Sampling branch changed unexpectedly.");
                ReceiverStream.WriteLive(output, bytes, count, frequency, rate, gain, appliedGain, samplingMode);
            }
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
        finally { if (device != IntPtr.Zero) rtlsdr_close(device); }
    }
}
