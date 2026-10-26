namespace MRS;
internal static class FolderScanTests
{
    internal static void Run(Action<bool,string> check)
    {
        var folder=new FrequencyItem{Name="Railroad",IsFolder=true};
        var nested=new FrequencyItem{Name="Branch",IsFolder=true,ParentId=folder.Id};
        var a=new FrequencyItem{Name="A",ParentId=folder.Id,FrequencyHz=160230000,Mode="NFM",BandwidthHz=12500};
        var b=new FrequencyItem{Name="B",ParentId=nested.Id,FrequencyHz=161100000,Mode="AM",BandwidthHz=10000};
        var hidden=a with{Id=Guid.NewGuid(),Name="Hidden",Visible=false};
        var other=a with{Id=Guid.NewGuid(),ParentId=null};
        FrequencyItem[] items=[folder,a,nested,b,hidden,other];
        check(FolderScanPlan.Create(items,folder.Id,true,false).SequenceEqual(new[]{a,b,hidden}),"Folder scan preserves depth-first saved order, mode/filter, excludes unrelated entries");
        check(FolderScanPlan.Create(items,folder.Id,false,true).SequenceEqual(new[]{a}),"Folder scan direct-only and hidden exclusion");
        check(FolderScanPlan.Create(items.Select(i=>i.Id==folder.Id?i with{Visible=false}:i),nested.Id,true,true).Length==0,"Folder scan hidden ancestor exclusion");
        var snapshot=FolderScanPlan.Create(items,folder.Id,true,false);items[1]=a with{FrequencyHz=1};
        check(snapshot[0].FrequencyHz==160230000,"Folder scan uses immutable snapshot when library changes");
        var timing=new FolderScanTiming();timing.Reset(0);
        check(!timing.Advance(.9,false,false,1,1,15)&&timing.Advance(1,false,false,1,1,15),"Folder scan quiet channel dwell");
        timing.Reset(0);check(!timing.Advance(1,true,false,1,1,15)&&!timing.Advance(1.9,false,false,1,1,15)&&timing.Advance(2,false,false,1,1,15),"Folder scan waits quiet delay after activity");
        timing.Reset(0);check(!timing.Advance(14,true,false,1,1,15)&&timing.Advance(15,true,false,1,1,15),"Folder scan maximum hold advances continuous control channel");
        timing.Reset(0);check(!timing.Advance(40,true,true,1,1,15)&&timing.Advance(41,true,false,1,1,15),"Folder scan explicit hold overrides maximum until released");
    }
}
