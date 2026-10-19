namespace MRS;
public partial class MainWindow
{
    private Task RenderPagingSmoke(Action<string> capture)
    {
        Stop();Packets.ClearRows();Packets.SetFilter("");Packets.IncludeFailed(false);Packets.PagingEnabled=true;
        if(!pagingDecoder.Enabled || !receiver.CaptureData)throw new InvalidOperationException("Paging switch did not enable discriminator capture");
        var source=new PacketContext("synthetic-paging-ui","Generated paging demonstration",true,929937500,2);
        const string p="{\"demod_name\":\"POCSAG1200\",\"address\":1234567,\"function\":3,\"alpha\":\"MRS SYNTHETIC TEST PAGE\",\"sync\":true}";
        const string f="{\"baud\":1600,\"level\":4,\"phase\":\"A\",\"cycle\":1,\"frame\":2,\"capcode\":1234567,\"msg_type\":\"alphanumeric\",\"fragment\":\"fragment_start\",\"k_ok\":true,\"message\":\"MRS SYNTHETIC FLEX FRAGMENT\"}";
        Packets.Add(PagingJson.Parse(p,source)!);Packets.Add(PagingJson.Parse(f,source)!);Packets.Add(PagingJson.Parse(f.Replace("true","false"),source)!);
        WorkspaceTabs.SelectedItem=PacketsTab;RefreshPackets();Packets.SelectFirst();UpdateLayout();
        if(Packets.VisibleCount!=2 || !Packets.ExportJson().Contains("DecoderJson"))throw new InvalidOperationException("Paging validation filter/export failed");
        capture("ui-paging.png");
        Packets.ClearRows();
        var complete=PagingJson.Parse(f,source)! with{Kind="alphanumeric · complete",DisplaySummary="Synthetic maintenance alert: inspect the equipment status and acknowledge this demonstration message. This long payload should wrap naturally within the Summary column without horizontal scrolling."};
        Packets.Add(complete);Packets.Add(complete with{Destination="1234568",Kind="alphanumeric · reassembled"});
        Packets.SetPagingRules([new("FLEX",source.FrequencyHz,"Capcode","1234567","Maintenance"),new("FLEX",source.FrequencyHz,"Capcode","1234568","Maintenance")]);
        UpdateLayout();capture("ui-paging-management.png");
        var inspector=new DataInspectorWindow(DataInspectionTests.Grant()){Owner=this};inspector.Show();inspector.UpdateLayout();
        var image=new System.Windows.Media.Imaging.RenderTargetBitmap((int)inspector.ActualWidth,(int)inspector.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);image.Render(inspector);
        var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));
        using(var file=System.IO.File.Create(System.IO.Path.Combine(AppContext.BaseDirectory,"ui-data-inspector.png")))encoder.Save(file);
        inspector.Close();
        var reference=new DataInspection(DataInspectionTests.Grant());
        var comparisonWindow=new DataInspectorWindow(reference.Observation with{Information="Changed synthetic message",RawHex="8000041002123501234572B8"},reference){Owner=this};
        comparisonWindow.Show();comparisonWindow.ShowComparison();comparisonWindow.UpdateLayout();
        var comparisonImage=new System.Windows.Media.Imaging.RenderTargetBitmap((int)comparisonWindow.ActualWidth,(int)comparisonWindow.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);comparisonImage.Render(comparisonWindow);
        var comparisonEncoder=new System.Windows.Media.Imaging.PngBitmapEncoder();comparisonEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(comparisonImage));
        using(var file=System.IO.File.Create(System.IO.Path.Combine(AppContext.BaseDirectory,"ui-data-comparison.png")))comparisonEncoder.Save(file);
        comparisonWindow.Close();
        if(Packets.VisibleCount!=1)throw new InvalidOperationException("Paging duplicate UI grouping failed");
        Packets.SetPagingRules([]);Packets.ClearRows();Packets.Add(PagingJson.Parse(p,source)!);Packets.Add(PagingJson.Parse(f,source)!);Packets.Add(PagingJson.Parse(f.Replace("true","false"),source)!);
        Packets.IncludeFailed(true);if(Packets.VisibleCount!=3)throw new InvalidOperationException("Failed FLEX fragment not inspectable");
        Packets.IncludeFailed(false);Packets.PagingEnabled=false;Packets.ClearRows();WorkspaceTabs.SelectedIndex=0;
        return Task.CompletedTask;
    }
}
