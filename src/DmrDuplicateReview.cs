using System.Text.RegularExpressions;

namespace MRS;

public sealed partial class FrequencyLibrary
{
    private const int DiscoveryMatchHz = 1000;

    private static (string Variant, string Network, string Site) DmrScope(string scope)
    {
        var parts = scope.Split(':');
        if (parts.Length != 4 || parts[0] != "DMR") return ("", "", "");
        string variant = parts[1] is "Capacity Plus" or "Connect Plus" or "DMR Tier III" ? parts[1] : "";
        string network = Regex.Match(parts[2], @"^Network (\d+)$").Groups[1].Value;
        string site = Regex.Match(parts[3], @"^Site (\d+)$").Groups[1].Value;
        return (variant, network, site);
    }

    private static bool DmrScopesAgree(string first, string second)
    {
        var a = DmrScope(first);
        var b = DmrScope(second);
        static bool Agree(string x, string y) => x.Length == 0 || y.Length == 0 || x == y;
        return Agree(a.Variant, b.Variant) && Agree(a.Network, b.Network) && Agree(a.Site, b.Site);
    }

    private static bool SameDmrCarrier(FrequencyItem first, FrequencyItem second)
    {
        if (!CompatibleIdentity(first, new("DMR", [], second.SiteScope, second.FrequencyHz, "", "",
                second.Signaling, Evidence: second.EvidenceStatus)))
            return false;

        var a = DmrScope(first.SiteScope);
        var b = DmrScope(second.SiteScope);
        if (a.Network.Length > 0 && a.Site.Length > 0 && a.Network == b.Network && a.Site == b.Site)
            return true;
        var cc = Regex.Match(first.Signaling, @"\bCC\s+(\d+)\b", RegexOptions.IgnoreCase);
        if (cc.Success && cc.Groups[1].Value ==
            Regex.Match(second.Signaling, @"\bCC\s+(\d+)\b", RegexOptions.IgnoreCase).Groups[1].Value)
            return true;
        return first.FrequencyHz == second.FrequencyHz && first.Signaling.Length > 0 &&
            first.Signaling.Equals(second.Signaling, StringComparison.OrdinalIgnoreCase);
    }

    internal LibraryReview PreviewDmrDuplicates(IEnumerable<Guid>? selected = null)
    {
        var next = items.ToList();
        var changes = new List<LibraryChange>();
        var vacated = new HashSet<Guid>();
        var chosen = selected?.ToHashSet();
        var candidates = items.Where(i => !i.IsFolder && i.Protocols == "DMR")
            .OrderBy(i => i.FrequencyHz).ToArray();
        bool Selected(FrequencyItem item) =>
            chosen == null || chosen.Contains(item.Id) || chosen.Any(id => Below(item, id, items));
        string Display(FrequencyItem item) => $"{DiscoveryNames.Frequency(item.FrequencyHz)} MHz · {item.Name} · {item.Id:N}";

        // Treat close neighbors as one review group. A chain spanning more than
        // the match tolerance is ambiguous, even if each neighboring pair fits.
        for (int start = 0, end; start < candidates.Length; start = end)
        {
            end = start + 1;
            while (end < candidates.Length &&
                   candidates[end].FrequencyHz - candidates[end - 1].FrequencyHz <= DiscoveryMatchHz)
                end++;
            var group = candidates[start..end];
            if (group.Length < 2 || !group.Any(Selected)) continue;
            var retained = group.OrderByDescending(DmrIdentityRank)
                .ThenBy(DmrGeneratedName).First();
            var combined = retained;
            string? conflict = null;
            if (!group.All(Selected))
                conflict = "Select all nearby candidates to review this group together.";
            else if (group[^1].FrequencyHz - group[0].FrequencyHz > DiscoveryMatchHz)
                conflict = "The nearby carriers span more than 1 kHz.";
            else if (group.SelectMany((item, index) => group.Skip(index + 1)
                .Select(other => SameDmrCarrier(item, other))).Any(agree => !agree))
                conflict = "Decoded identities compete or there is too little shared identity evidence.";
            else
            {
                foreach (var item in group.Where(i => i.Id != retained.Id))
                {
                    combined = MergeDmrDuplicate(combined, item, out var fields);
                    if (fields.Count == 0) continue;
                    conflict = "Different saved values for " + string.Join(", ", fields) + ".";
                    break;
                }
            }
            if (conflict != null)
            {
                foreach (var item in group)
                    changes.Add(new("Conflict", Display(item), conflict + " " + item.SiteScope + " " + item.Signaling));
                continue;
            }

            next[next.IndexOf(retained)] = combined;
            foreach (var item in group.Where(i => i.Id != retained.Id))
            {
                next.Remove(item);
                var parent = item.ParentId;
                while (parent is Guid id && vacated.Add(id))
                    parent = items.Single(f => f.Id == id).ParentId;
                changes.Add(new("Merge", Display(item), $"Keep {Display(combined)} · {combined.SiteScope} · {combined.Signaling}"));
            }
        }

        var emptyPlaceholders = items.Where(i => i.IsFolder && vacated.Contains(i.Id) &&
            (i.Name.StartsWith("Unassigned at ", StringComparison.OrdinalIgnoreCase) || i.Name == "Unresolved site") &&
            i.Notes.Length == 0 && i.Tags.Length == 0 && i.MappingStatus.Length == 0 && !i.ScannerFolder &&
            RemovableFolder(i, next)).ToList();
        while (emptyPlaceholders.Where(f => !next.Any(i => i.ParentId == f.Id)).ToArray() is { Length: > 0 } empty)
        {
            foreach(var folder in empty)
            {
                next.Remove(folder);
                emptyPlaceholders.Remove(folder);
                changes.Add(new("Remove empty folder", folder.Name, "Generated DMR folder emptied by the merge."));
            }
        }
        Validate(next);
        return new(ReviewStamp, next, records.ToList(), changes, PersistentRollback: true);
    }

    private static int DmrIdentityRank(FrequencyItem item)
    {
        var scope = DmrScope(item.SiteScope);
        return (scope.Network.Length > 0 && scope.Site.Length > 0 ? 8 : 0) +
            (scope.Variant.Length > 0 ? 4 : 0) + (item.SiteScope.Length > 0 ? 2 : 0) +
            (item.EvidenceStatus.Length > 0 && !DmrProvisional(item.EvidenceStatus) ? 1 : 0);
    }

    private static bool DmrGeneratedName(FrequencyItem item) => DiscoveryNames.DmrRolePlaceholder(item) ||
        DiscoveryNames.Generated(item.Name) &&
        (item.Name == DiscoveryNames.Label(item) || Regex.IsMatch(item.Name,
            @"^(Observed carrier|Mapped voice carrier|Unknown signal|Signal [\d.,]+ MHz|Channel \d+-\d+|(?:DMR|Cap\+|Con\+)(?: · (?:IDs unknown|Net [\dA-F]+|Site \d+|CC \d+|Control|Rest|Voice|Ch [\dA-F, -]+))+)$",
            RegexOptions.IgnoreCase));

    private static FrequencyItem MergeDmrDuplicate(FrequencyItem retained, FrequencyItem other, out List<string> conflicts)
    {
        static string Union(string a, string b, string separator) => string.Join(separator,
            (a + separator + b).Split(separator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase));
        bool newer = other.ChannelLastSeen > retained.ChannelLastSeen || retained.ChannelLastSeen == null;
        var merged = MergeItem(retained, other with
        {
            Name = "", Notes = "", Signaling = "", ChannelIds = "", ChannelRoles = "", DmrIdentifiers = "",
            ChannelLastSeen = null, LastReceivedOnFrequencyUtc = null, LastObservedFrequencyHz = null, OriginalFrequencyHz = null,
            EvidenceStatus = "", DetectionDate = retained.DetectionDate ?? other.DetectionDate
        }, out conflicts);
        bool retainedGenerated = DmrGeneratedName(retained), otherGenerated = DmrGeneratedName(other);
        if (!retainedGenerated && !otherGenerated && retained.Name != other.Name)
            conflicts.Add(nameof(FrequencyItem.Name));
        string dmr = Union(retained.DmrIdentifiers, other.DmrIdentifiers, "\n");
        if (dmr.Length > 20608) conflicts.Add(nameof(FrequencyItem.DmrIdentifiers));
        string notes = retained.Notes == other.Notes || other.Notes.Length == 0 ? retained.Notes :
            retained.Notes.Length == 0 ? other.Notes : retained.Notes + "\n\n" + other.Notes;
        return merged with
        {
            Name = retainedGenerated && !otherGenerated ? other.Name : retained.Name,
            Notes = notes,
            Signaling = Union(retained.Signaling, other.Signaling, "; "),
            ChannelIds = Union(retained.ChannelIds, other.ChannelIds, ", "),
            ChannelRoles = Union(retained.ChannelRoles, other.ChannelRoles, "; "),
            DmrIdentifiers = dmr,
            ChannelLastSeen = newer ? other.ChannelLastSeen : retained.ChannelLastSeen,
            LastReceivedOnFrequencyUtc = other.LastReceivedOnFrequencyUtc > retained.LastReceivedOnFrequencyUtc
                ? other.LastReceivedOnFrequencyUtc : retained.LastReceivedOnFrequencyUtc ?? other.LastReceivedOnFrequencyUtc,
            LastObservedFrequencyHz = newer
                ? other.LastObservedFrequencyHz ?? other.FrequencyHz
                : retained.LastObservedFrequencyHz ?? retained.FrequencyHz
        };
    }
}
