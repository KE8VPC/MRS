using System.Text.RegularExpressions;
namespace MRS;
internal sealed record NxdnIdentity(int System,int Site,int Ran,string Location);
internal sealed class NxdnControlParser
{
    private bool valid;private int ran=-1;private string location="";
    internal void Reset(){valid=false;ran=-1;location="";}
    internal NxdnIdentity? Feed(string text)
    {
        if(text.Contains("Sync:")){valid=text.Contains("NXDN48")||text.Contains("NXDN96");valid&=text.Contains("CAC ")&&!text.Contains("CRC ERR")&&(text.Contains("SRV_INFO")||text.Contains("SITE_INFO"));location="";var r=Regex.Match(text,@"RAN\s+(\d+)");ran=r.Success?int.Parse(r.Groups[1].Value):-1;}
        if(!valid||text.Contains("CRC ERR")){if(text.Contains("CRC ERR"))valid=false;return null;}
        var l=Regex.Match(text,@"Location ID \[([0-9A-Fa-f]{6})\]");if(l.Success)location=l.Groups[1].Value;
        var m=Regex.Match(text,@"Sys Code:\s*(\d+)\s*-\s*Site Code\s*(\d+)");if(!m.Success||ran<0||ran>63||location.Length!=6)return null;
        valid=false;return new(int.Parse(m.Groups[1].Value),int.Parse(m.Groups[2].Value),ran,location);
    }
}
internal sealed record NxdnReception(NxdnIdentity Identity,PacketContext Context,DateTimeOffset Seen,long Epoch=0);
