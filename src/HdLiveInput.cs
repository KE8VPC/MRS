using System.IO;
using System.Threading.Channels;
namespace MRS;
internal sealed class HdLiveInput(long frequency)
{
    private readonly Channel<IqFrame> frames=Channel.CreateBounded<IqFrame>(new BoundedChannelOptions(32){SingleReader=true,SingleWriter=true});
    internal void Offer(IqFrame frame)
    {
        if(!frames.Writer.TryWrite(frame with {Data=(byte[])frame.Data.Clone()}))frames.Writer.TryComplete(new IOException("HD input queue overflow; receiver remains running."));
    }
    internal void Complete()=>frames.Writer.TryComplete();
    internal async Task Feed(Stream stream,CancellationToken cancel)
    {
        HdIqConverter? converter=null;int rate=0;
        await foreach(var frame in frames.Reader.ReadAllAsync(cancel))
        {
            if(frame.Frequency!=frequency || frame.Channel is not {Mode:"WFM"} channel || channel.Frequency!=frequency || frame.Capture.UsableBandwidthHz<600000 || frame.Format!=IqSampleFormat.Unsigned8 || rate!=0 && frame.Rate!=rate)
                throw new IOException("Receiver configuration changed; restart HD on the new capture.");
            converter??=new(frame.Rate);rate=frame.Rate;await stream.WriteAsync(converter.Process(frame.Data),cancel);
        }
    }
}
