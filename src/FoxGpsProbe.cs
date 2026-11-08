using System.IO;
namespace MRS;
internal static class FoxGpsProbe
{
 internal static async Task<string> Run(string port,int baud)
 {
  using var gps=new FoxGps();try{await gps.Connect(port,baud);for(int i=0;i<40;i++){await Task.Delay(200);if(gps.Fix is {} fix&&fix.Fresh(DateTimeOffset.UtcNow))return "PASS: live NMEA GPS read through the M.R.S. serial backend\n"+gps.Status+$"\nFresh coordinate fix: yes; satellites: {fix.Satellites?.ToString()??"not yet supplied"}; HDOP: {fix.Hdop?.ToString()??"not yet supplied"}.\nExact coordinates omitted. No RF receiver opened, no RF transmission and no position upload.";}throw new IOException("No fresh NMEA position within 8 seconds. "+gps.Status);}finally{await gps.Disconnect();}
 }
}
