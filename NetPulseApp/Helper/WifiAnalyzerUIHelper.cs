// Helper/WifiAnalyzerUIHelper.cs
namespace NetPulseApp.Helper
{
    public enum SignalLevel { Excellent, Good, Fair, Weak, Poor }

    /// <summary>
    /// Signal-quality levels, colours and tips for the Wi-Fi page. Trimmed
    /// copy of WiFiPasswordViewer's WifiAnalyzerUIHelper — the chart-series
    /// palette and control factories stay behind until the v1.1 nearby
    /// networks / channels tabs need them.
    /// </summary>
    public static class WifiAnalyzerUIHelper
    {
        public static readonly Color Excellent = Color.FromArgb(16, 185, 129);
        public static readonly Color Good = Color.FromArgb(132, 204, 22);
        public static readonly Color Fair = Color.FromArgb(245, 158, 11);
        public static readonly Color Weak = Color.FromArgb(249, 115, 22);
        public static readonly Color Poor = Color.FromArgb(239, 68, 68);

        public static SignalLevel LevelFromRssi(int rssi) => rssi switch
        {
            >= -50 => SignalLevel.Excellent,
            >= -60 => SignalLevel.Good,
            >= -67 => SignalLevel.Fair,
            >= -75 => SignalLevel.Weak,
            _ => SignalLevel.Poor
        };

        public static Color LevelColor(SignalLevel level) => level switch
        {
            SignalLevel.Excellent => Excellent,
            SignalLevel.Good => Good,
            SignalLevel.Fair => Fair,
            SignalLevel.Weak => Weak,
            _ => Poor
        };

        public static Color RssiColor(int rssi) => LevelColor(LevelFromRssi(rssi));

        public static string LevelLabel(int rssi) => LevelFromRssi(rssi).ToString();

        public static string LevelTip(SignalLevel level) => level switch
        {
            SignalLevel.Excellent => "Excellent signal. Everything should work at full speed.",
            SignalLevel.Good => "Good signal. Calls and HD streaming should work well.",
            SignalLevel.Fair => "Fair signal. Fine for browsing; video calls may sometimes drop quality.",
            SignalLevel.Weak => "Weak signal. Move closer to the router or remove obstacles.",
            _ => "Poor signal. Expect slow speeds and disconnects."
        };
    }
}
