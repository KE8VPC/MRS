using System.Text.RegularExpressions;
namespace MRS;

internal sealed class HdMetadata
{
    internal string Station {get;private set;}="";
    internal string Title {get;private set;}="";
    internal string Artist {get;private set;}="";
    internal bool Locked {get;private set;}
    internal SortedSet<int> Services {get;}=[];
    internal string Summary=>$"{(Locked?"Synchronized":"Searching")} · {(Station.Length>0?Station:"Station name pending")}\n{Title}{(Artist.Length>0?" — "+Artist:"")}\nAdvertised audio services: {(Services.Count>0?string.Join(", ",Services.Select(n=>$"HD{n+1}")):"pending")}";
    internal bool Accept(string line)
    {
        string before=Summary;
        // Pinned nrsc5 CLI output uses an HH:mm:ss prefix. Unknown lines remain in the diagnostic log.
        string text=Regex.Replace(line,@"^\d{2}:\d{2}:\d{2}\s+","");
        if(text=="Synchronized")Locked=true;
        else if(text.StartsWith("Lost synchronization",StringComparison.OrdinalIgnoreCase)){Locked=false;Station=Title=Artist="";Services.Clear();}
        else if(text.StartsWith("Station name: "))Station=text[14..].Trim();
        else if(text.StartsWith("Title: "))Title=text[7..].Trim();
        else if(text.StartsWith("Artist: "))Artist=text[8..].Trim();
        else if(Regex.Match(text,@"^Audio service (\d+):") is {Success:true} match && int.TryParse(match.Groups[1].Value,out int service) && service<8)Services.Add(service);
        return before!=Summary;
    }
}
