using System.Text.Json;
namespace MRS;
internal static class ReceiverIdentity
{
    internal static string? Key(RtlNative.Device device)
    {
        string serial = device.Serial.Trim();
        if (serial.Length < 2 || serial == "12345678" || serial.All(c => c == '0') || (uint.TryParse(serial, out uint number) && number <= 2)) return null;
        return JsonSerializer.Serialize(new[] { device.Backend == "RTL" ? device.Manufacturer : device.Backend + ":" + device.Manufacturer, device.Product, serial });
    }
    internal static RtlNative.Device? Match(IEnumerable<RtlNative.Device> devices, string? key)
    {
        if (key == null) return null;
        var matches = devices.Where(d => Key(d) == key).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
    internal static string? UniqueKey(IEnumerable<RtlNative.Device> devices, RtlNative.Device selected)
    {
        string? key = Key(selected); var match = Match(devices, key); return match?.Index == selected.Index && match.Backend == selected.Backend ? key : null;
    }
}
