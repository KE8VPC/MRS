using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace MRS;

// Only invoked in an owned child process. No TX exports or firmware update functions are loaded.
internal sealed class HackRfNative
{
    internal static string LibraryFolder => Path.Combine(AppContext.BaseDirectory, "Receivers", "HackRF");
    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Init();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr ListDevices();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FreeList(IntPtr list);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int OpenList(IntPtr list, int index, out IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int DeviceAction(IntPtr device);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetFrequency(IntPtr device, ulong hz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetRate(IntPtr device, double rate);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetUInt(IntPtr device, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int SetByte(IntPtr device, byte value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate uint Filter(uint rate);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int Callback(IntPtr transfer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int StartRx(IntPtr device, Callback callback, IntPtr context);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr ErrorName(int code);
    [StructLayout(LayoutKind.Sequential)] internal struct DeviceList { internal IntPtr Serials, BoardIds, Indices; internal int Count; internal IntPtr UsbDevices; internal int UsbCount; }
    [StructLayout(LayoutKind.Sequential)] internal struct Transfer { internal IntPtr Device, Buffer; internal int BufferLength, ValidLength; internal IntPtr RxContext, TxContext; }
    private readonly IntPtr library;
    private T Get<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, "hackrf_" + name));
    private HackRfNative()
    {
        library = LoadLibraryEx(Path.Combine(LibraryFolder, "hackrf.dll"), IntPtr.Zero, 0x1100);
        if (library == IntPtr.Zero) throw new DllNotFoundException("HackRF library unavailable. Keep the Receivers/HackRF folder beside MRS.exe. Windows loader error " + Marshal.GetLastWin32Error());
        foreach (string symbol in new[] {"init","exit","device_list","device_list_free","device_list_open","close","error_name","start_rx","stop_rx","is_streaming","set_antenna_enable","set_sample_rate","compute_baseband_filter_bw_round_down_lt","set_baseband_filter_bandwidth","set_freq","set_lna_gain","set_vga_gain","set_amp_enable"})
            NativeLibrary.GetExport(library,"hackrf_"+symbol);
        foreach (int rate in ReceiverCapabilities.HackRf.SampleRates)
            if (Get<Filter>("compute_baseband_filter_bw_round_down_lt")((uint)rate) != HackRfProtocol.UsableBandwidth(rate)) throw new IOException("HackRF native filter table does not match the adapter.");
    }
    private void Check(int result, string action)
    {
        if (result != 0) throw new IOException($"{action}: {Marshal.PtrToStringAnsi(Get<ErrorName>("error_name")(result))} ({result}). Select HackRF mode in Mayhem, check WinUSB, and close other SDR applications.");
    }
    private static List<RtlNative.Device> ReadList(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero) throw new IOException("HackRF device enumeration failed.");
        var list = Marshal.PtrToStructure<DeviceList>(pointer);
        if (list.Count < 0 || list.Count > 128) throw new IOException("Invalid HackRF device count.");
        var result = new List<RtlNative.Device>();
        for (int n = 0; n < list.Count; n++)
        {
            string serial = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(list.Serials, n * IntPtr.Size)) ?? "";
            int board = Marshal.ReadInt32(list.BoardIds, n * sizeof(int));
            // USB board ID 0x6089 is HackRF One (including compatible PortaPack combinations).
            if (board == 0x6089) result.Add(new((uint)n, "HackRF One / PortaPack", "HackRF", "HackRF One", serial, "HackRF"));
        }
        return result;
    }
    internal static int Run(string[] args)
    {
        try
        {
            if (args[0] == "--hackrf-fixture") { HackRfProtocol.Fixture(long.Parse(args[2]), int.Parse(args[4]), int.Parse(args[3]), int.Parse(args[6])); return 0; }
            var native = new HackRfNative(); native.Check(native.Get<Init>("init")(), "Initialize HackRF");
            try
            {
                IntPtr list = native.Get<ListDevices>("device_list")();
                try
                {
                    var devices = ReadList(list);
                    if (args[0] == "--hackrf-probe") { Console.Write(JsonSerializer.Serialize(devices)); return 0; }
                    if (args[0] != "--hackrf-worker" || args.Length < 6) throw new ArgumentException("Invalid HackRF worker arguments.");
                    uint index = uint.Parse(args[1]);
                    var selected = devices.SingleOrDefault(d => d.Index == index) ?? throw new IOException("Selected HackRF is no longer present.");
                    string? identity = args[5].Length == 0 ? null : Encoding.UTF8.GetString(Convert.FromBase64String(args[5]));
                    if (identity != null && ReceiverIdentity.UniqueKey(devices, selected) != identity) throw new IOException("HackRF identity changed; select the receiver again.");
                    native.Check(native.Get<OpenList>("device_list_open")(list, (int)index, out var device), "Open HackRF");
                    try { native.Receive(device, long.Parse(args[2]), int.Parse(args[4]), int.Parse(args[3])); }
                    finally { native.Get<DeviceAction>("close")(device); }
                    return 0;
                }
                finally { if (list != IntPtr.Zero) native.Get<FreeList>("device_list_free")(list); }
            }
            finally { native.Get<Init>("exit")(); }
        }
        catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
    private void Receive(IntPtr device, long frequency, int rate, int gain)
    {
        using var output = Console.OpenStandardOutput(); var commands = new HackRfProtocol.Commands();
        using var queue = new BlockingCollection<byte[]>(32);
        string? failure = null;
        Callback callback = pointer =>
        {
            try
            {
                var transfer = Marshal.PtrToStructure<Transfer>(pointer);
                if (transfer.ValidLength <= 0 || transfer.ValidLength > transfer.BufferLength || transfer.ValidLength > 1048576 || transfer.ValidLength % 2 != 0)
                    throw new IOException("Invalid HackRF USB transfer.");
                var data = new byte[transfer.ValidLength]; Marshal.Copy(transfer.Buffer, data, 0, data.Length);
                if (!queue.TryAdd(data)) throw new IOException("HackRF receive backlog overflow. Reception stopped to avoid a silent I/Q gap; lower the sample rate or reduce CPU/disk load.");
                return 0;
            }
            catch (Exception ex) { Interlocked.CompareExchange(ref failure, ex.Message, null); return -1; }
        };
        var start = Get<StartRx>("start_rx"); var stop = Get<DeviceAction>("stop_rx");
        var streaming = Get<DeviceAction>("is_streaming");
        void Configure()
        {
            if (!ReceiverCapabilities.HackRf.CanTune(frequency) || !ReceiverCapabilities.HackRf.SampleRates.Contains(rate)) throw new ArgumentException("Unsupported HackRF tuning or rate.");
            var gains = HackRfGain.Decode(gain);
            Check(Get<SetByte>("set_antenna_enable")(device, 0), "Disable antenna power");
            Check(Get<SetRate>("set_sample_rate")(device, rate), "Set sample rate");
            uint filter = Get<Filter>("compute_baseband_filter_bw_round_down_lt")((uint)rate);
            if (filter != HackRfProtocol.UsableBandwidth(rate)) throw new IOException("HackRF library returned an unexpected analog filter width.");
            filter = (uint)HackRfProtocol.CaptureBandwidth(rate,gain);
            Check(Get<SetUInt>("set_baseband_filter_bandwidth")(device, filter), "Set baseband filter");
            Check(Get<SetFrequency>("set_freq")(device, (ulong)frequency), "Tune HackRF");
            Check(Get<SetUInt>("set_lna_gain")(device, (uint)gains.Lna), "Set LNA gain");
            Check(Get<SetUInt>("set_vga_gain")(device, (uint)gains.Vga), "Set VGA gain");
            Check(Get<SetByte>("set_amp_enable")(device, (byte)(gains.Amplifier ? 1 : 0)), "Set RF amplifier");
        }
        try
        {
            Configure(); Check(start(device, callback, IntPtr.Zero), "Start HackRF RX");
            var block = new byte[32768]; int filled = 0;
            while (!commands.Stopped)
            {
                if (Volatile.Read(ref failure) is { } error) throw new IOException(error);
                var next = commands.Take();
                if (next.Frequency != 0 || next.Rate != 0 || next.Gain != 0)
                {
                    Check(stop(device), "Pause HackRF RX for control change");
                    while (queue.TryTake(out _)) { } filled = 0;
                    if (next.Frequency != 0) frequency = next.Frequency;
                    if (next.Rate != 0) rate = next.Rate;
                    if (next.Gain != 0) gain = next.Gain;
                    Configure(); Check(start(device, callback, IntPtr.Zero), "Resume HackRF RX");
                }
                if (!queue.TryTake(out var data, 100))
                { if (streaming(device) != 1) throw new IOException("HackRF USB stream ended."); continue; }
                HackRfProtocol.Normalize(data);
                for (int offset = 0; offset < data.Length;)
                {
                    int count = Math.Min(block.Length - filled, data.Length - offset);
                    Buffer.BlockCopy(data, offset, block, filled, count); filled += count; offset += count;
                    if (filled == block.Length) { HackRfProtocol.Write(output, block, frequency, rate, gain); filled = 0; }
                }
            }
        }
        finally { stop(device); Get<SetByte>("set_amp_enable")(device, 0); GC.KeepAlive(callback); }
    }
}
