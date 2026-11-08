namespace MRS;
// FLEX_NEXT add_ch() represents control characters as printable escapes before cJSON encoding.
// This is a presentation-only pass. DecoderJson/Information remain original evidence.
internal static class PagingText
{
 internal static string Readable(PacketObservation packet,string text)=>packet.Protocol=="FLEX"&&packet.Kind.StartsWith("alphanumeric",System.StringComparison.OrdinalIgnoreCase)?Expand(text):text;
 internal static string Expand(string text){var b=new System.Text.StringBuilder();for(int i=0;i<text.Length;i++){if(text[i]=='\\'&&i+1<text.Length&&text[i+1] is 'r' or 'n' or 't'){char c=text[++i];b.Append(c=='r'?'\r':c=='n'?'\n':'\t');}else b.Append(text[i]);}return b.ToString().Replace("\r\n","\n").Replace('\r','\n');}
 internal static string Compact(string text)=>System.Text.RegularExpressions.Regex.Replace(text,"[\\r\\n\\t]+"," ").Trim();
}
