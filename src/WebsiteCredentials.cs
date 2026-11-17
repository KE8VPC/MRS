using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
namespace MRS;
// Windows user-bound encryption; no plaintext token is written to settings or exports.
internal static class WebsiteCredentials
{
 [StructLayout(LayoutKind.Sequential)]private struct Blob{public int Size;public IntPtr Data;}
 [DllImport("crypt32.dll",SetLastError=true,CharSet=CharSet.Unicode)]private static extern bool CryptProtectData(ref Blob input,string description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("crypt32.dll",SetLastError=true)]private static extern bool CryptUnprotectData(ref Blob input,IntPtr description,IntPtr entropy,IntPtr reserved,IntPtr prompt,uint flags,out Blob output);
 [DllImport("kernel32.dll")]private static extern IntPtr LocalFree(IntPtr memory);
 internal static byte[] Transform(byte[] bytes,bool protect)
 {
  var input=new Blob{Size=bytes.Length,Data=Marshal.AllocHGlobal(bytes.Length)};Blob output=default;
  try{Marshal.Copy(bytes,0,input.Data,bytes.Length);bool ok=protect?CryptProtectData(ref input,"M.R.S. website sync",IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output):CryptUnprotectData(ref input,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,1,out output);if(!ok)throw new IOException("Windows could not "+(protect?"protect":"unlock")+" the device token",new Win32Exception(Marshal.GetLastWin32Error()));var result=new byte[output.Size];Marshal.Copy(output.Data,result,0,result.Length);return result;}
  finally{Marshal.Copy(new byte[bytes.Length],0,input.Data,bytes.Length);Marshal.FreeHGlobal(input.Data);if(output.Data!=IntPtr.Zero)LocalFree(output.Data);}
 }
 internal static void Save(string path,string token){byte[] plain=Encoding.UTF8.GetBytes(token);try{var encrypted=Transform(plain,true);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path+".tmp",encrypted);File.Move(path+".tmp",path,true);}finally{CryptographicOperations.ZeroMemory(plain);}}
 internal static string Read(string path){if(!File.Exists(path))return "";if(new FileInfo(path).Length>4096)throw new IOException("Invalid protected token file");byte[] plain=Transform(File.ReadAllBytes(path),false);try{return Encoding.UTF8.GetString(plain);}finally{CryptographicOperations.ZeroMemory(plain);}}
}
