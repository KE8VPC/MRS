using System.Globalization;

using System.IO;

using System.Text.Json;



namespace MRS;

public sealed record SignalActivity(DateTimeOffset FirstSeen,DateTimeOffset LastSeen,long Hits,double OccupiedHz,double AboveFloorDb,string Receiver);

public sealed record FrequencyItem

{

    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid? ParentId { get; init; }

    public bool IsFolder { get; init; }

    public string Name { get; init; } = "";

    public long FrequencyHz { get; init; }

    public string Mode { get; init; } = "NFM";

    public double BandwidthHz { get; init; }

    public string Modulation { get; init; } = "";

    public string Protocols { get; init; } = "";

    public string Signaling { get; init; } = "";

    public string Notes { get; init; } = "";

    public string Tags { get; init; } = "";

    public string Color { get; init; } = "#71CDF5";

    public bool Visible { get; init; } = true;

    public bool ScannerFolder {get;init;}

    public string? DetectionDate {get;init;}

    public DateTimeOffset? ChannelLastSeen{get;init;}public string ChannelRoles{get;init;}="";public string SiteScope {get;init;}="";public string ChannelIds{get;init;}="";public long? UplinkHz{get;init;}public string EvidenceStatus{get;init;}="";

    public string DmrIdentifiers {get;init;}="";

    public SignalActivity? Activity {get;init;}

    public override string ToString() => IsFolder ? Name : $"{Name} · {FrequencyHz / 1000000m:0.######} MHz · {Mode} {Protocols}";

}

public sealed partial class FrequencyLibrary

{

    private sealed record Document(int Version, List<FrequencyItem> Items, bool ReceiverBImported = false);

    private bool receiverBImported;

    public static event Action<FrequencyLibrary, string>? Saved;

    private readonly string path;

    private string? revision;

    private List<FrequencyItem> items;

    private readonly Stack<List<FrequencyItem>> undo = new();

    public IReadOnlyList<FrequencyItem> Items => items.AsReadOnly();

    public bool CanUndo => undo.Count > 0;

    public FrequencyLibrary(string path)

    {

        this.path = path;

        revision = File.Exists(path) ? File.ReadAllText(path) : null;

        if (revision != null)

        {

            var document = JsonSerializer.Deserialize<Document>(revision) ?? throw new IOException("Empty frequency library.");

            if (document.Version != 1) throw new IOException("Unsupported frequency-library version.");

            receiverBImported = document.ReceiverBImported;

            items = document.Items ?? throw new IOException("Missing frequency entries.");

        }

        else items = [];

        Validate(items);

    }

    public static void Validate(List<FrequencyItem> values)

    {

        var index = new Dictionary<Guid, FrequencyItem>();

        foreach (var node in values)

        {

            if (node == null || node.Id == Guid.Empty || !index.TryAdd(node.Id, node)) throw new IOException("Invalid or duplicate entry ID.");

            if (string.IsNullOrWhiteSpace(node.Name) || node.Name.Length > 160) throw new IOException("Names must contain 1–160 characters.");

            if (!node.IsFolder && (node.FrequencyHz < 1 || node.FrequencyHz > 1000000000000)) throw new IOException("Frequency must be between 1 Hz and 1 THz.");

            if (!double.IsFinite(node.BandwidthHz) || node.BandwidthHz < 0 || node.BandwidthHz > 1000000000) throw new IOException("Invalid saved bandwidth.");

            if (node.Mode == null || node.Modulation == null || node.Protocols == null || node.Signaling == null || node.Notes == null || node.Tags == null) throw new IOException("Missing entry fields.");

            if(node.DmrIdentifiers is { } ids&&(ids.Length>20608))throw new IOException("Invalid DMR identifier list.");

            if (node.Color == null || !System.Text.RegularExpressions.Regex.IsMatch(node.Color, "^#[0-9a-fA-F]{6}$")) throw new IOException("Color must be #RRGGBB.");

        }

        foreach (var node in values)

        {

            var visited = new HashSet<Guid> { node.Id }; Guid? parent = node.ParentId;

            while (parent is { } id)

            {

                if (!index.TryGetValue(id, out var folder) || !folder.IsFolder) throw new IOException("The destination folder does not exist.");

                if (!visited.Add(id)) throw new IOException("A folder cannot be moved inside itself or one of its children.");

                if (visited.Count > 64) throw new IOException("Folder nesting is limited to 64 levels.");

                parent = folder.ParentId;

            }

        }

    }

    private void Commit(List<FrequencyItem> next, bool keepUndo = true)

    {

        Validate(next);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var fileLock = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        string? disk = File.Exists(path) ? File.ReadAllText(path) : null;

        if (disk != revision) throw new IOException("The library changed in another window. Click Reload before editing again.");

        string json = JsonSerializer.Serialize(new Document(1, next, receiverBImported), new JsonSerializerOptions { WriteIndented = true });

        string temp = path + ".tmp";

        File.WriteAllText(temp, json);

        if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true); else File.Move(temp, path);

        if (keepUndo) undo.Push(items);

        items = next; revision = json;

        fileLock.Dispose();

        Saved?.Invoke(this, Path.GetFullPath(path));

    }

    public void ImportReceiverB(string legacyPath)

    {

        if (receiverBImported || !File.Exists(legacyPath)) return;

        var legacy = new FrequencyLibrary(legacyPath);

        var next = items.ToList();

        if (legacy.Items.Count > 0)

        {

            var folder = new FrequencyItem { IsFolder = true, Name = "Imported from Receiver B" };

            var ids = legacy.Items.ToDictionary(x => x.Id, _ => Guid.NewGuid());

            next.Add(folder);

            next.AddRange(legacy.Items.Select(x => x with { Id = ids[x.Id], ParentId = x.ParentId is { } parent ? ids[parent] : folder.Id }));

        }

        receiverBImported = true;

        try { Commit(next, false); }

        catch { receiverBImported = false; throw; }

    }

    public void Upsert(FrequencyItem value)

    {

        var next = items.ToList(); int index = next.FindIndex(n => n.Id == value.Id);

        if (index < 0) next.Add(value); else next[index] = value;

        Commit(next);

    }

    internal void SaveDiscoveries(IEnumerable<ActivityDiscovery> observations,string receiver)

    {

        var next=items.ToList();bool changed=false;

        foreach(var observation in observations)

        {

            if(observation.Frequency<=0||observation.Hits<0||!double.IsFinite(observation.OccupiedHz)||!double.IsFinite(observation.AboveFloorDb))continue;

            double tolerance=Math.Min(observation.FilterHz*.3,5000);

            string date=observation.LastSeen.ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);

            var item=next.Where(i=>!i.IsFolder&&(i.DetectionDate==null||i.DetectionDate==date)&&Math.Abs(i.FrequencyHz-observation.Frequency)<=tolerance).MinBy(i=>Math.Abs(i.FrequencyHz-observation.Frequency));

            if(item==null)

            {

                if(next.Count(i=>i.Activity!=null)>=1000)throw new IOException("Discovery limit of 1,000 entries reached. Remove unwanted discoveries before saving more.");

                var folder=next.FirstOrDefault(i=>i.IsFolder&&i.ScannerFolder);

                if(folder==null){folder=new(){Name="Discovered signals",IsFolder=true,ScannerFolder=true};next.Add(folder);}

                var day=next.FirstOrDefault(i=>i.IsFolder&&i.ParentId==folder.Id&&i.DetectionDate==date);

                if(day==null){day=new(){Name=date,IsFolder=true,ParentId=folder.Id,DetectionDate=date};next.Add(day);}

                item=new(){Name=$"Signal {observation.Frequency/1e6:0.######} MHz",FrequencyHz=observation.Frequency,ParentId=day.Id,DetectionDate=date,

                    Mode=observation.Mode,BandwidthHz=observation.FilterHz,Modulation="Unknown",Protocols="Unknown",Tags="RF activity discovery"};

                next.Add(item);

            }

            var previous=item.Activity;

            var activity=new SignalActivity(previous?.FirstSeen<observation.FirstSeen?previous.FirstSeen:observation.FirstSeen,

                previous?.LastSeen>observation.LastSeen?previous.LastSeen:observation.LastSeen,

                Math.Min(long.MaxValue-observation.Hits,previous?.Hits??0)+observation.Hits,

                observation.OccupiedHz,observation.AboveFloorDb,receiver);

            int index=next.FindIndex(i=>i.Id==item.Id);next[index]=item with{Activity=activity};changed=true;

        }

        // Background metadata does not create an unbounded Undo history.

        if(changed)Commit(next,false);

    }

    internal void SaveNxdn(NxdnReception observation,string receiver)
    {
        if(observation.Context.Replay||observation.Context.FrequencyHz is not {} hz||hz<=0)return;
        var item=items.Where(i=>!i.IsFolder&&Math.Abs(i.FrequencyHz-hz)<=1000).MinBy(i=>Math.Abs(i.FrequencyHz-hz));
        if(item==null){SaveDiscoveries([new ActivityDiscovery(Guid.NewGuid(),hz,"NFM",8000,8000,observation.Seen,observation.Seen,1,0)],receiver);item=items.Where(i=>!i.IsFolder&&Math.Abs(i.FrequencyHz-hz)<=1000).MinBy(i=>Math.Abs(i.FrequencyHz-hz));}if(item==null)return;
        string id=$"NXDN SYS {observation.Identity.System} SITE {observation.Identity.Site} RAN {observation.Identity.Ran}";
        if(item.Signaling.Contains(id,StringComparison.Ordinal))return;
        string protocols=item.Protocols is "" or "Unknown"?"NXDN":item.Protocols.Contains("NXDN",StringComparison.OrdinalIgnoreCase)?item.Protocols:item.Protocols+"; NXDN";
        string signaling=(item.Signaling+"; "+id).Trim(';',' ');if(signaling.Length>4000)throw new IOException("Signaling metadata limit reached; export before adding more identifiers.");
        var next=items.ToList();next[next.FindIndex(i=>i.Id==item.Id)]=item with{Protocols=protocols,Signaling=signaling};Commit(next,false);
    }

    internal void SaveDmr(DmrReception reception,string receiver)

    {

        if(reception.Context.Replay||reception.Context.FrequencyHz is not {} hz||hz<=0)return;

        var channel=SiteChannelData.FromDmr(reception);if(channel!=null)SaveSiteChannels([channel]);
    }


    public void Move(Guid id, Guid? parent)

    {

        var item = items.Single(n => n.Id == id);

        Upsert(item with { ParentId = parent });

    }

    public void Reorder(Guid id, int direction)

    {

        var next = items.ToList(); var item = next.Single(n => n.Id == id);

        var siblings = next.Where(n => n.ParentId == item.ParentId).ToList(); int sibling = siblings.FindIndex(n => n.Id == id) + direction;

        if (sibling < 0 || sibling >= siblings.Count) return;

        int from = next.IndexOf(item), to = next.IndexOf(siblings[sibling]);

        (next[from], next[to]) = (next[to], next[from]); Commit(next);

    }

    public void Delete(Guid id)

        =>DeleteMany([id]);

    public void DeleteMany(IEnumerable<Guid> ids)

    {

        var remove = ids.Where(id=>items.Any(i=>i.Id==id)).ToHashSet();

        if(remove.Count==0)return;

        bool changed;

        do { changed = false; foreach (var item in items) if (item.ParentId is { } parent && remove.Contains(parent)) changed |= remove.Add(item.Id); } while (changed);

        Commit(items.Where(n => !remove.Contains(n.Id)).ToList());

    }

    public void SetVisibility(IEnumerable<Guid> ids,bool visible)

    {

        var chosen=ids.ToHashSet();if(!items.Any(i=>chosen.Contains(i.Id)&&i.Visible!=visible))return;

        Commit(items.Select(i=>chosen.Contains(i.Id)?i with{Visible=visible}:i).ToList());

    }

    public void Undo() { if (undo.TryPeek(out var previous)) { Commit(previous, false); undo.Pop(); } }

    public IEnumerable<FrequencyItem> VisibleEntries()

    {

        var index = items.ToDictionary(n => n.Id);

        foreach (var item in items.Where(n => !n.IsFolder && n.Visible))

        {

            Guid? parent = item.ParentId; bool visible = true;

            while (parent is { } id) { var folder = index[id]; if (!folder.Visible) { visible = false; break; } parent = folder.ParentId; }

            if (visible) yield return item;

        }

    }

    public static bool TryFrequency(string mhz, out long hz)

    {

        hz = 0;

        if (!decimal.TryParse(mhz, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value) || value < .000001m || value > 1000000m) return false;

        decimal scaled = value * 1000000m;

        if (scaled != decimal.Truncate(scaled)) return false;

        hz = (long)scaled; return true;

    }

}



