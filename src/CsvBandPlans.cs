using System.Globalization;
using System.IO;
using Microsoft.VisualBasic.FileIO;
namespace MRS;
internal static class PlanCsv
{
    internal static List<Dictionary<string,string>> Read(string path)
    {
        if(new FileInfo(path).Length>4*1024*1024)throw new InvalidDataException("CSV exceeds 4 MB");
        using var parser=new TextFieldParser(path,System.Text.Encoding.UTF8){TextFieldType=FieldType.Delimited,HasFieldsEnclosedInQuotes=true,TrimWhiteSpace=false};parser.SetDelimiters(",");
        var headers=parser.ReadFields()??throw new InvalidDataException("Missing header");headers=headers.Select(h=>h.Trim().ToLowerInvariant()).ToArray();
        if(headers.Any(string.IsNullOrEmpty)||headers.Distinct().Count()!=headers.Length)throw new InvalidDataException("Empty/duplicate headers");
        var rows=new List<Dictionary<string,string>>();while(!parser.EndOfData){var values=parser.ReadFields();if(values==null)continue;if(values.Length!=headers.Length)throw new InvalidDataException($"Row {rows.Count+2}: wrong field count");if(rows.Count==10000)throw new InvalidDataException("CSV exceeds 10000 rows");rows.Add(headers.Select((h,i)=>(h,values[i])).ToDictionary(v=>v.h,v=>v.Item2,StringComparer.OrdinalIgnoreCase));}return rows;
    }
    internal static string Get(Dictionary<string,string> r,string k)=>r.GetValueOrDefault(k,"");
    internal static long? Hz(Dictionary<string,string> r,string name,bool optional=false)
    {
        foreach(var (suffix,multiplier) in new[]{("_hz",1m),("_khz",1000m),("_mhz",1000000m)})if(r.TryGetValue(name+suffix,out var s)&&!string.IsNullOrWhiteSpace(s)){
            if(!decimal.TryParse(s,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var v)||v*multiplier!=decimal.Truncate(v*multiplier)||Math.Abs(v*multiplier)>1000000000000m)throw new InvalidDataException("Invalid frequency: "+name);return (long)(v*multiplier);}
        if(optional)return null;throw new InvalidDataException("Missing frequency: "+name);
    }
    internal static string MHz(long? hz)=>hz.HasValue?(hz.Value/1000000m).ToString("0.######",CultureInfo.InvariantCulture):"";
    internal static string Quote(string s)=>"\""+s.Replace("\"","\"\"")+"\"";
    internal static void Write(string path,IEnumerable<Dictionary<string,string>> values,string[] required)
    {
        var rows=values.ToArray();var headers=required.Concat(rows.SelectMany(r=>r.Keys)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var w=new StreamWriter(temp,false,new System.Text.UTF8Encoding(false))){w.WriteLine(string.Join(",",headers.Select(Quote)));foreach(var row in rows)w.WriteLine(string.Join(",",headers.Select(h=>Quote(Get(row,h)))));}File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
internal sealed record CsvPlanMarker(string Plan,long Low,long High,string Label,string Detail,bool Grid=false,bool Uplink=false,string Color="");
internal sealed class CsvBandPlans
{
    internal string Root{get;}internal List<CsvPlanMarker> Markers=[];internal List<string> Plans=[];internal string Error="";internal HashSet<string> Hidden=new();internal bool Uplinks,Grid;
    private readonly List<(string Plan,int Id,long Base,long Step,long? Offset,int Slots,int First,int Last)> rules=[];
    internal CsvBandPlans(string root){Root=root;}
    internal void Reload()
    {
        var markers=new List<CsvPlanMarker>();var loaded=new List<string>();rules.Clear();var errors=new List<string>();
        if(!Directory.Exists(Root)){Error="Bandplan folder does not exist";return;}
        var options=new EnumerationOptions{RecurseSubdirectories=true,AttributesToSkip=FileAttributes.ReparsePoint,IgnoreInaccessible=false};
        int fileCount=0;try{foreach(var file in Directory.EnumerateFiles(Root,"*.csv",options).Take(1001)){
            if(++fileCount>1000){errors.Add("Folder exceeds 1000 CSV files; remaining files skipped");break;}
            string name=Path.GetFileName(file).ToLowerInvariant();if(name is not("bands.csv" or "channels.csv" or "spacing.csv"))continue;
            string plan=Path.GetRelativePath(Root,Path.GetDirectoryName(file)!).Replace('\\','/');var pending=new List<CsvPlanMarker>();
            int ruleStart=rules.Count;try{var rows=PlanCsv.Read(file);foreach(var row in rows){string label=PlanCsv.Get(row,"label"),source=PlanCsv.Get(row,"source"),color=PlanCsv.Get(row,"color");if(color.Length>0&&!System.Text.RegularExpressions.Regex.IsMatch(color,@"^#[0-9a-fA-F]{6}$"))throw new InvalidDataException("Color must be #RRGGBB");
                if(name=="bands.csv"){long low=PlanCsv.Hz(row,"start")!.Value,high=PlanCsv.Hz(row,"end")!.Value;if(low<=0||high<low)throw new InvalidDataException("Invalid band limits");pending.Add(new(plan,low,high,label,source,Color:color));}
                if(name=="channels.csv"){long down=PlanCsv.Hz(row,"downlink")!.Value;long? up=PlanCsv.Hz(row,"uplink",true);if(down<=0||up is <=0)throw new InvalidDataException("Invalid channel frequency");string lcn=PlanCsv.Get(row,"lcn");string text=string.IsNullOrWhiteSpace(label)?(lcn.Length>0?lcn:PlanCsv.MHz(down)+" MHz"):label;string detail=$"{plan} · {lcn}\nDownlink {PlanCsv.MHz(down)} MHz · uplink {(up.HasValue?PlanCsv.MHz(up):"unknown")} MHz\n{PlanCsv.Get(row,"role")} · last seen {PlanCsv.Get(row,"last_seen")}\n{source}\n{PlanCsv.Get(row,"conflict")}";pending.Add(new(plan,down,down,text,detail,Color:color));if(up.HasValue)pending.Add(new(plan,up.Value,up.Value,text+" UL",detail,false,true,color));}
                if(name=="spacing.csv"){int Int(string key,int fallback){string s=PlanCsv.Get(row,key);return s==""?fallback:int.Parse(s,CultureInfo.InvariantCulture);}int id=Int("identifier",0),slots=Int("slots_per_carrier",1),first=Int("first_channel",0),last=Int("last_channel",4095);long b=PlanCsv.Hz(row,"base")!.Value,step=PlanCsv.Hz(row,"spacing")!.Value;long? offset=PlanCsv.Hz(row,"offset",true);if(id<0||id>15||slots<1||slots>4||first<0||last>4095||first>last||b<=0||step<=0)throw new InvalidDataException("Invalid spacing rule");rules.Add((plan,id,b,step,offset,slots,first,last));}
            }if(markers.Count+pending.Count>50000||rules.Count>50000)throw new InvalidDataException("Folder exceeds 50000 markers");markers.AddRange(pending);loaded.Add(plan);}catch(Exception ex){rules.RemoveRange(ruleStart,rules.Count-ruleStart);errors.Add($"{plan}/{name}: {ex.Message}");}
        }}catch(Exception ex){errors.Add(ex.Message);}
        Markers=markers;Plans=loaded.Distinct().Order().ToList();Error=string.Join("\n",errors.Take(12));
    }
    internal IEnumerable<CsvPlanMarker> Visible(double low,double high,DisplaySettings settings)
    {
        bool Enabled(string p)=>!Hidden.Contains(p)&&(p switch{"reference/us"=>settings.ShowBandPlan,"reference/arrl"=>settings.ShowArrlPlan,"reference/nifog"=>settings.ShowNifogPlan,"reference/tv"=>settings.ShowTvPlan,_=>true});
        foreach(var m in Markers)if(Enabled(m.Plan)&&(!m.Uplink||Uplinks)&&(m.High==m.Low?m.Low>=low&&m.Low<=high:m.Low<high&&m.High>low))yield return m;
        if(!Grid)yield break;
        foreach(var r in rules.Where(r=>Enabled(r.Plan))){long start=Math.Max(r.First/r.Slots,(long)Math.Ceiling((low-r.Base)/r.Step)),end=Math.Min(r.Last/r.Slots,(long)Math.Floor((high-r.Base)/r.Step));if(end-start>300)continue;
            for(long n=start;n<=end;n++){long hz=r.Base+n*r.Step;yield return new(r.Plan,hz,hz,$"{r.Id:00}-{n*r.Slots:0000} grid",$"{r.Plan}\nCalculated spacing only; not an observed channel",true);}}
    }
    internal static void Seed(string root)
    {
        RailroadPlan.Seed(root);BroadcastChannels.Seed(root);
        Dictionary<string,string> Band(long a,long b,string label,string source)=>new(){["start_mhz"]=PlanCsv.MHz(a),["end_mhz"]=PlanCsv.MHz(b),["label"]=label,["source"]=source};
        Dictionary<string,string> Channel(long hz,string label,string source)=>new(){["downlink_mhz"]=PlanCsv.MHz(hz),["label"]=label,["source"]=source};
        void Save(string name,IEnumerable<Dictionary<string,string>> rows,string[] fields){string path=Path.Combine(root,name);if(!File.Exists(path))PlanCsv.Write(path,rows,fields);}
        Save("reference/us/bands.csv",UsBandPlan.Bands.Select(b=>Band(b.Low,b.High,b.Name,b.Source)),["start_mhz","end_mhz","label","source"]);
        Save("reference/us/channels.csv",UsBandPlan.Channels.Select(c=>Channel(c.Frequency,c.Name,c.Source)),["downlink_mhz","label","source"]);
        Save("reference/arrl/bands.csv",ArrlPlan.Entries.Select(b=>Band(b.Low,b.High,b.Label,ArrlPlan.Source+"; "+b.Note)),["start_mhz","end_mhz","label","source"]);
        Save("reference/nifog/channels.csv",NifogPlan.Markers.Select(c=>Channel(c.Frequency,c.Name,c.Source)),["downlink_mhz","label","source"]);
        Save("reference/tv/bands.csv",TvBandPlan.Channels.Select(c=>Band(c.Low,c.High,$"TV RF {c.Number}",c.Details(false))),["start_mhz","end_mhz","label","source"]);
    }
}
