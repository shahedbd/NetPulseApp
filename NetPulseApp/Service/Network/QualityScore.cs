// Service/Network/QualityScore.cs
namespace NetPulseApp.Service.Network
{
    /// <summary>
    /// Turns ping stats (latency, jitter, loss) into an A–F connection grade
    /// and a "Good for: Gaming / Calls / 4K" list. Pure math — no I/O, so the
    /// Dashboard and the upcoming Diagnose page share one set of thresholds.
    /// </summary>
    public static class QualityScore
    {
        /// <summary>Upper bounds per grade A, B, C, D — anything above D is F.</summary>
        private static readonly double[] LatencyMs = { 30, 60, 100, 200 };
        private static readonly double[] JitterMs = { 5, 10, 20, 40 };
        private static readonly double[] LossPct = { 0.5, 1, 2.5, 5 };

        private static readonly char[] Grades = { 'A', 'B', 'C', 'D', 'F' };

        /// <summary>Use-case thresholds: max latency (ms), max jitter (ms), max loss (%).</summary>
        private static readonly (string Name, double Latency, double Jitter, double Loss)[] UseCases =
        {
            ("Gaming", 50, 15, 1),
            ("Calls", 150, 30, 2),
            ("4K", 300, 50, 3)
        };

        /// <summary>Minimum replies before a grade means anything.</summary>
        public const int MinSamples = 3;

        /// <summary>The worst per-metric grade, or null until enough replies are in.</summary>
        public static char? Grade(PingStats stats)
        {
            if (stats == null || stats.Sent < MinSamples)
                return null;
            if (stats.Received < MinSamples)
                return 'F'; // mostly timeouts — the loss alone fails it

            int worst = Math.Max(Tier(stats.Avg, LatencyMs),
                        Math.Max(Tier(stats.Jitter, JitterMs), Tier(stats.LossPercent, LossPct)));
            return Grades[worst];
        }

        /// <summary>"Gaming · Calls · 4K", or "—" when nothing qualifies / too few samples.</summary>
        public static string GoodFor(PingStats stats)
        {
            if (stats == null || stats.Received < MinSamples)
                return "—";

            var fits = UseCases
                .Where(u => stats.Avg <= u.Latency && stats.Jitter <= u.Jitter && stats.LossPercent <= u.Loss)
                .Select(u => u.Name)
                .ToList();

            return fits.Count == 0 ? "—" : string.Join(" · ", fits);
        }

        /// <summary>A/B = good, C = fair, D/F = poor — mapped to theme colors by the UI.</summary>
        public static QualityLevel Level(char? grade) => grade switch
        {
            'A' or 'B' => QualityLevel.Good,
            'C' => QualityLevel.Fair,
            'D' or 'F' => QualityLevel.Poor,
            _ => QualityLevel.Unknown
        };

        private static int Tier(double value, double[] bounds)
        {
            for (int i = 0; i < bounds.Length; i++)
                if (value <= bounds[i]) return i;
            return bounds.Length;
        }
    }

    public enum QualityLevel { Unknown, Good, Fair, Poor }
}
