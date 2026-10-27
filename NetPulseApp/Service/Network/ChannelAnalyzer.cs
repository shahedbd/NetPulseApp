// Service/Network/ChannelAnalyzer.cs
using NetPulseApp.Model;

namespace NetPulseApp.Service.Network
{
    /// <summary>
    /// Pure channel math: frequency → band/channel and channel spans.
    /// Trimmed copy of WiFiPasswordViewer's ChannelAnalyzer — the 2.4 GHz
    /// congestion score and per-band recommendations come with the v1.1
    /// channels tab.
    /// </summary>
    public static class ChannelAnalyzer
    {
        public static (WifiBand Band, int Channel) FromFrequency(int mhz)
        {
            if (mhz == 2484) return (WifiBand.Ghz24, 14);
            if (mhz >= 2412 && mhz <= 2472) return (WifiBand.Ghz24, (mhz - 2407) / 5);
            if (mhz >= 5150 && mhz <= 5895) return (WifiBand.Ghz5, (mhz - 5000) / 5);
            if (mhz >= 5955 && mhz <= 7115) return (WifiBand.Ghz6, (mhz - 5950) / 5);
            return (WifiBand.Unknown, 0);
        }

        /// <summary>Lowest and highest 20 MHz channel a BSS occupies.</summary>
        public static (int Low, int High) Span(WifiBand band, int channel, int width, int secondaryOffset = 0)
        {
            if (band == WifiBand.Ghz24)
            {
                if (width < 40 || secondaryOffset == 0) return (channel, channel);
                return secondaryOffset > 0 ? (channel, channel + 4) : (channel - 4, channel);
            }

            int n = Math.Max(1, width / 20);
            int baseCh = band == WifiBand.Ghz6 ? 1 : channel >= 149 ? 149 : channel >= 100 ? 100 : 36;
            int idx = (channel - baseCh) / 4;
            int start = baseCh + (idx - idx % n) * 4;
            return (start, start + (n - 1) * 4);
        }
    }
}
