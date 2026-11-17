using System.IO;

namespace MRS;

internal static class DmrSavingTests
{
    internal static void Run(Action<bool,string> check, string root, SiteChannelData provisional, SiteChannelData decoded)
    {
        var seen = DateTimeOffset.UtcNow;
        provisional = provisional with { Seen = seen };
        decoded = decoded with { Hz = provisional.Hz + 300, Seen = seen.AddSeconds(3) };
        var library = new FrequencyLibrary(Path.Combine(root, "dmr-deferred.json"));
        library.SaveSiteChannels([provisional]);
        var first = library.Items.Single(i => !i.IsFolder);
        library.Upsert(first with { Name = "DMR · My named carrier", Notes = "Keep my notes." });

        // The editor defers the flush, but the site queue still accepts evidence.
        var queued = new Dictionary<string, SiteChannelData>();
        provisional.QueueForSave(queued);
        decoded.QueueForSave(queued);
        (decoded with { Evidence = "Provisional identity", Seen = seen.AddSeconds(4) }).QueueForSave(queued);
        library.SaveSiteChannels(queued.Values);
        var upgraded = library.Items.Single(i => !i.IsFolder);
        check(upgraded.Id == first.Id && upgraded.SiteScope == decoded.Scope &&
              upgraded.EvidenceStatus == decoded.Evidence && upgraded.LastObservedFrequencyHz == decoded.Hz &&
              upgraded.FrequencyHz == first.FrequencyHz && upgraded.Name == "DMR · My named carrier" &&
              upgraded.Notes == "Keep my notes." && upgraded.ParentId != first.ParentId &&
              library.Items.Single(i => i.Id == upgraded.ParentId).Name == "Site 1" &&
              library.PendingDiscoveries.Count == 0,
            "A deferred DMR decode upgrades the original entry and managed folder without losing its ID, center or custom fields");

        library = new FrequencyLibrary(Path.Combine(root, "dmr-legacy-review.json"));
        library.SaveSiteChannels([provisional]);
        var legacy = library.Items.Single(i => !i.IsFolder);
        library.Upsert(legacy with { Notes = "Early observation", ChannelRoles = "Rest", ChannelIds = "LCN 3" });
        library.SaveSiteChannels([decoded], forceNew:true);
        var identified = library.Items.Single(i => i.SiteScope == decoded.Scope);
        library.Upsert(identified with { Name = "My system carrier", Notes = "Confirmed observation" });
        library.SaveSiteChannels([decoded], forceNew:true);
        (decoded with { Seen = seen.AddSeconds(5) }).QueueForSave(queued = []);
        library.SaveSiteChannels(queued.Values);
        check(library.PendingDiscoveries.Count == 1, "A legacy DMR duplicate group holds an otherwise matching live observation");

        string path = library.LibraryPath;
        string original = File.ReadAllText(path);
        var selection = library.PreviewDmrDuplicates([legacy.Id]);
        check(selection.Conflicts > 0 && selection.Channels.Count(i => !i.IsFolder) == 3,
            "A selected-only legacy DMR review surfaces nearby unselected candidates");
        var plan = library.PreviewDmrDuplicates();
        var survivor = plan.Channels.Single(i => !i.IsFolder);
        check(File.ReadAllText(path) == original && plan.Changes.Count(c => c.Action == "Merge") == 2 &&
              survivor.Id == identified.Id && survivor.Name == "My system carrier" &&
              survivor.Notes.Contains("Early observation") && survivor.Notes.Contains("Confirmed observation") &&
              survivor.ChannelIds == "LCN 3" && survivor.ChannelRoles.Contains("Rest") &&
              !plan.Channels.Any(i => i.IsFolder && i.Name.StartsWith("Unassigned at ")),
            "DMR duplicate preview keeps the decoded record ID, combines evidence and notes, and prunes emptied placeholders");
        string backup = library.ApplyReview(plan);
        new FrequencyLibrary(path).RollbackOrganization(backup);
        check(File.ReadAllText(path) == original, "DMR cleanup backup restores the exact original library after reopening");
        library = new FrequencyLibrary(path);
        library.ApplyReview(library.PreviewDmrDuplicates());
        library.SaveSiteChannels([decoded with { Seen = seen.AddSeconds(6) }]);
        check(library.Items.Single(i => !i.IsFolder).Id == identified.Id && library.PendingDiscoveries.Count == 0,
            "A fresh matching decode enriches the survivor and clears its held observation after cleanup");

        var competing = decoded with { Identifiers = decoded.Identifiers.Replace("CC 6","CC 7") };
        var another = competing with { Identifiers = decoded.Identifiers.Replace("CC 6","CC 8") };
        queued.Clear();
        competing.QueueForSave(queued);
        another.QueueForSave(queued);
        library.SaveSiteChannels(queued.Values);
        library.SaveSiteChannels([decoded]);
        check(library.PendingDiscoveries.Count == 2 && library.PendingDiscoveries.Select(p => p.Key).Distinct().Count() == 2,
            "Competing DMR identifiers at the same scope and frequency survive a later matching decode");

        var conflict = new FrequencyLibrary(Path.Combine(root, "dmr-conflicting-sites.json"));
        conflict.SaveSiteChannels([decoded]);
        conflict.SaveSiteChannels([decoded with {
            Scope = decoded.Scope.Replace("Site 1","Site 2"),
            Folders = [.. decoded.Folders.Take(3), "Site 2"],
            Identifiers = decoded.Identifiers.Replace("Site 1","Site 2")
        }], forceNew:true);
        var conflictPlan = conflict.PreviewDmrDuplicates();
        check(conflictPlan.Conflicts == 2 && conflictPlan.Channels.Count(i => !i.IsFolder) == 2,
            "Competing decoded DMR sites at the same frequency stay separate in the review");
    }
}
