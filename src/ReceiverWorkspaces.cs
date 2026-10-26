using System.IO;
using System.Windows;
namespace MRS;

// Ownership is shared by the two windows in this application process.
internal static class ReceiverOwnership
{
    private sealed record Claim(object Owner, uint Index, string? Identity, string Label, string Backend);
    private static readonly List<Claim> claims = new();
    internal static bool Acquire(object owner, uint index, string? identity, string label, out string message, string backend = "RTL")
    {
        lock(claims)
        {
            var conflict=claims.FirstOrDefault(c=>!ReferenceEquals(c.Owner,owner) && c.Backend == backend &&
                (identity!=null && c.Identity!=null ? identity==c.Identity : index==c.Index));
            message=conflict==null?"":$"This device is in use by {conflict.Label}. Select the other receiver, or stop that source first.";
            if(conflict!=null)return false;
            claims.RemoveAll(c=>ReferenceEquals(c.Owner,owner));claims.Add(new(owner,index,identity,label,backend));return true;
        }
    }
    internal static void Release(object owner){lock(claims)claims.RemoveAll(c=>ReferenceEquals(c.Owner,owner));}
}

public partial class MainWindow
{
    private readonly string workspaceSlot;
    private readonly bool diagnosticWorkspace;
    private void OpenOtherReceiver(object sender,RoutedEventArgs e)
    {
        OpenFleetManager();
    }
    private bool ClaimReceiver(RtlNative.Device device,string? identity)
    {
        if(ReceiverOwnership.Acquire(this,device.Index,identity,$"Receiver {workspaceSlot}",out string message,device.Backend))return true;
        RtlStatus.Text=message;Log(message);return false;
    }
    private async Task RenderDualReceiverSmoke(Action<string> capture)
    {
        Stop();
        var other=new MainWindow(true,"B"){ShowInTaskbar=false,Left=-16000};other.Show();
        try
        {
            if(dataFolder==other.dataFolder)throw new InvalidOperationException("Receiver profiles share a directory.");
            var d=new RtlNative.Device(0,"Synthetic A","MRS","Test","DUAL-A");
            var b=d with{Index=1,Serial="DUAL-B"};
            RtlDevices.ItemsSource=new[]{d,b};RtlDevices.SelectedItem=d;other.RtlDevices.ItemsSource=new[]{d,b};other.RtlDevices.SelectedItem=b;
            if(!ClaimReceiver(d,ReceiverIdentity.Key(d)) || other.ClaimReceiver(d,ReceiverIdentity.Key(d)) || !other.ClaimReceiver(b,ReceiverIdentity.Key(b)))throw new InvalidOperationException("Receiver ownership guard failed.");
            SpectrumTitle.Text="RF SPECTRUM / WATERFALL · SYNTHETIC A";other.SpectrumTitle.Text="RF SPECTRUM / WATERFALL · SYNTHETIC B";
            Spectrum.Configure(DisplaySettings.Theme("Dark"));
            rfMode=true;engine.StartExternal(2);other.rfMode=true;other.engine.StartExternal(2);
            FrequencyBox.Text="146.520000";other.FrequencyBox.Text="147.000000";
            receiver.Start(0,146520000,-1000,false,fixture:true,sampleRate:240000,fixtureSeconds:20);
            other.receiver.Start(1,147000000,0,false,fixture:true,sampleRate:960000,fixtureSeconds:20);
            for(int i=0;i<300 && (receiver.AppliedFrequency==0 || other.receiver.AppliedFrequency==0);i++)await Task.Delay(20);
            if(receiver.AppliedFrequency!=146520000 || other.receiver.AppliedFrequency!=147000000)throw new InvalidOperationException("Independent workers did not start.");
            string a=Path.Combine(dataFolder,"dual-a.mrsiq"),bb=Path.Combine(other.dataFolder,"dual-b.mrsiq");
            receiver.RecordIq(a,new IqMetadata(3,DateTimeOffset.UtcNow,false,-1000,null,"NFM"));other.receiver.RecordIq(bb,new IqMetadata(3,DateTimeOffset.UtcNow,false,0,null,"NFM"));await Task.Delay(500);
            await receiver.Retune(146600000);await Task.Delay(200);
            if(other.receiver.AppliedFrequency!=147000000 || !other.receiver.IsRecording)throw new InvalidOperationException("Tuning A changed B.");
            UpdateLayout();capture("ui-dual-receiver-a.png");
            Stop();
            if(!other.engine.Active || !other.receiver.IsRecording)throw new InvalidOperationException("Stopping A stopped B.");
            other.UpdateLayout();
            var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)other.ActualWidth,(int)other.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(other);
            var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
            using(var stream=File.Create(Path.Combine(AppContext.BaseDirectory,"ui-dual-receiver-b.png")))encoder.Save(stream);
            other.Stop();
            using(var ar=new IqReader(a))if(ar.Read()==null)throw new InvalidOperationException("A recording empty.");
            using(var br=new IqReader(bb))if(br.Read()==null)throw new InvalidOperationException("B recording empty.");
            other.FrequencyBox.Text="145.825000";other.VolumeSlider.Value=.23;other.Packets.DecoderEnabled=true;other.Packets.SubaudibleEnabled=true;other.P25Packets.Grouped=false;other.P25Packets.HiddenP25Types=["NID header","TSBK"];
            string profile=other.dataFolder;other.Close();
            var restored=System.Text.Json.JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(profile,"settings.json")));
            if(restored?.Frequency!="145.825000" || !restored.PacketDecoding || !restored.SubaudibleDecoding || restored.GroupP25Repeats || restored.HiddenP25Types?.Length!=2 || Math.Abs(restored.Volume-.23)>.001)throw new InvalidOperationException("B settings were not saved independently.");
            var reopened=new MainWindow(true,"B",profile){ShowInTaskbar=false,Left=-16000};
            try{reopened.Show();if(reopened.FrequencyBox.Text!="145.825000" || !reopened.Packets.DecoderEnabled || !reopened.Packets.SubaudibleEnabled || !reopened.receiver.CaptureData || reopened.P25Packets.Grouped || !reopened.P25Packets.HiddenP25Types.SequenceEqual(new[]{"NID header","TSBK"}) || Math.Abs(reopened.VolumeSlider.Value-.23)>.001)throw new InvalidOperationException("B profile restore failed.");}finally{reopened.Close();}
        }
        finally{Stop();if(!other.closing)other.Close();}
    }
}



