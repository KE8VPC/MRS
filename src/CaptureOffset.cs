using System.IO;

namespace MRS;

internal sealed record CaptureOffset(bool Enabled = true, int Khz = 250)

{

    internal long Hz => Enabled ? Khz * 1000L : 0;

    internal long Center(ChannelSettings channel, int rate, int? gain = null)

    {

        if (Khz < -5000 || Khz > 5000) throw new IOException("Capture offset must be between -5000 and +5000 kHz.");

        long center = checked(channel.Frequency + Hz);

        if (!ReceiverCapabilities.HackRf.CanTune(channel.Frequency) || !ReceiverCapabilities.HackRf.CanTune(center))

            throw new IOException("Listening frequency plus capture offset exceeds the HackRF 1–6000 MHz range. Adjust or disable the offset.");

        if (!channel.Fits(center, gain is {} token ? HackRfProtocol.CaptureBandwidth(rate,token) : HackRfProtocol.UsableBandwidth(rate)))

            throw new IOException("Capture offset and receive filter do not fit the hardware capture filter. For HF filtering use an offset such as +250 kHz, or disable HF filtering.");

        return center;

    }

}

