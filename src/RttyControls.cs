using System.IO;
using System.Text;
using System.Windows.Controls;
using Microsoft.Win32;

namespace MRS;

public partial class MainWindow
{
    private RttyReceiver? rttyReceiver;
    private readonly StringBuilder rttyPending = new();
    private readonly TextBox rttyTranscript = new() { IsReadOnly=true, TextWrapping=System.Windows.TextWrapping.Wrap,
        VerticalScrollBarVisibility=ScrollBarVisibility.Auto, VerticalContentAlignment=System.Windows.VerticalAlignment.Top };
    private readonly TextBlock rttyStatus = ModeStatus();

    private void InitializeRtty(TabControl amateur)
    {
        AddModePage(amateur,"RTTY",rttyTranscript,out var tools,out var header);
        var baud=new ComboBox { ItemsSource=new[]{45.45,50,75},SelectedIndex=0,Width=75 };
        var mark=new TextBox { Text="2125",Width=70 };
        var shift=new ComboBox { ItemsSource=new[]{170,425,850},SelectedIndex=0,Width=70 };
        var reverse=new CheckBox { Content="Reverse",Foreground=System.Windows.Media.Brushes.Gainsboro };
        ModeFields(tools,("Baud",baud),("Mark Hz",mark),("Shift Hz",shift));
        tools.Children.Add(reverse);
        header.Children.Add(rttyStatus);
        rttyStatus.Text="Stopped · 5-bit US ITA2 · USB, LSB, AM, NFM or audio/WAV";
        ModeButton(tools,"Start decoding",()=>
        {
            if(rfMode&&receiver.AppliedChannel?.Mode is not ("USB" or "LSB" or "AM" or "NFM"))
                throw new IOException("Choose USB, LSB, AM or NFM before decoding RTTY audio.");
            var decoder=new RttyReceiver((double)baud.SelectedItem,ModeNumber(mark,"Mark",300,3500),
                (int)shift.SelectedItem,reverse.IsChecked==true);
            decoder.Character+=character=>{if(rttyPending.Length<4000)rttyPending.Append(character);};
            lock(receiveModeSync){rttyReceiver=decoder;rttyPending.Clear();}
            rttyStatus.Text="Decoding · match baud, tones and polarity to the signal";
        });
        ModeButton(tools,"Stop",()=>{lock(receiveModeSync)rttyReceiver=null;rttyStatus.Text="Stopped";});
        ModeButton(tools,"Clear text",()=>{lock(receiveModeSync)rttyPending.Clear();rttyTranscript.Clear();});
        ModeButton(tools,"Save text…",()=>
        {
            TickRtty();
            var picker=new SaveFileDialog{Filter="Text transcript|*.txt",FileName="MRS-RTTY-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")};
            if(picker.ShowDialog(this)==true)File.WriteAllText(picker.FileName,rttyTranscript.Text);
        });
    }

    private void TickRtty()
    {
        string text;
        lock(receiveModeSync)
        {
            text=rttyPending.ToString();rttyPending.Clear();
            if(rttyReceiver!=null)rttyStatus.Text=$"Decoding US ITA2 · {rttyReceiver.Characters} characters · {rttyReceiver.Rejected} rejected frames";
            else if(rttyStatus.Text.StartsWith("Decoding",StringComparison.Ordinal))rttyStatus.Text="Stopped · transcript retained";
        }
        if(text.Length==0)return;
        rttyTranscript.AppendText(text);
        if(rttyTranscript.Text.Length>30000)rttyTranscript.Text=rttyTranscript.Text[^24000..];
        rttyTranscript.ScrollToEnd();
    }
}
