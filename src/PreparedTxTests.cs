using System.IO;
using System.Text.Json;
namespace MRS;
internal static class PreparedTxTests
{
    internal static void Run(Action<bool,string> check)
    {
        var p=new WeakTxPlan("FT8",14074000,1500,"CQ K1ABC FN42",0,DateTimeOffset.UnixEpoch.AddSeconds(30));
        var tones=WeakWaveform.Encode(p);var expected=(byte[])tones.Clone();tones[0]^=1;
        check(WeakWaveform.Encode(p).SequenceEqual(expected),"TX prepared message cache cannot be corrupted by caller mutation");
        check(WeakWaveform.Encode(p with{AudioHz=1000,SlotUtc=p.SlotUtc.AddSeconds(30)}).SequenceEqual(expected),"TX tone preparation reused across slot/audio changes that do not change encoded symbols");
        check(!WeakWaveform.Encode(p with{Message="CQ K1ABC EM12"}).SequenceEqual(expected),"TX message changes select a different prepared symbol sequence");
        bool rejected=false;try{WeakWaveform.ValidateTones(p,new byte[2]);}catch(IOException){rejected=true;}
        check(rejected,"TX worker rejects incomplete prepared symbols");
        rejected=false;var invalid=(byte[])expected.Clone();invalid[0]=8;try{WeakWaveform.ValidateTones(p,invalid);}catch(IOException){rejected=true;}
        check(rejected,"TX worker rejects out-of-band prepared tone indices");
        var declaration=new TxDeclaration("United States","K1ABC","General",true,true,true);
        var permit=JsonSerializer.Deserialize<WeakTxPermit>(WeakTxAuthorization.Issue(declaration,p,"synthetic-device",expected))!;
        check(permit.Plan==p&&Convert.FromBase64String(permit.PreparedTones!).SequenceEqual(expected),"TX preparation crosses worker boundary with the exact authorized plan");
        check(WeakTxPlan.InitialHandoffLeadSeconds>WeakTxPlan.Ft8HandoffLeadSeconds&&WeakTxPlan.Ft8HandoffLeadSeconds==1.3,"TX first burst gets longer preparation without truncating established FT8 reply capture");
    }
}
