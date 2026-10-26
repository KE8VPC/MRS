using System.IO;
namespace MRS;
internal static class RailroadPlan
{
    internal static void SeedExpanded(string root)
    {
        const string prefix="MRS.ExpandedRailroad.";
        foreach(string resource in typeof(RailroadPlan).Assembly.GetManifestResourceNames().Where(n=>n.StartsWith(prefix,StringComparison.Ordinal)))
        {string suffix=resource[prefix.Length..];int dot=suffix.IndexOf('.');string path=Path.Combine(root,"railroad-expanded",suffix[..dot],suffix[(dot+1)..]);if(File.Exists(path))continue;using var stream=typeof(RailroadPlan).Assembly.GetManifestResourceStream(resource)!;using var reader=new StreamReader(stream);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,reader.ReadToEnd());}
    }
    internal static void Seed(string root)
    {
        foreach(string name in new[]{"channels.csv","bands.csv"})
        {
            string path=Path.Combine(root,"reference","railroad",name);
            if(File.Exists(path))continue;
            using var stream=typeof(RailroadPlan).Assembly.GetManifestResourceStream("MRS.Railroad."+name)??throw new IOException("Missing railroad reference dataset");
            using var reader=new StreamReader(stream);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,reader.ReadToEnd());
        }
    }
}
