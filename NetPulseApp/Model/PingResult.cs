namespace NetPulseApp.Model
{
    /// <summary>Immutable result for a single ICMP echo reply.</summary>
    public sealed class PingResult
    {
        public int Sequence { get; init; }
        public string Host { get; init; } = string.Empty;
        public string ResolvedIp { get; init; } = string.Empty;
        public bool Success { get; init; }
        public long RoundtripMs { get; init; }
        public int Ttl { get; init; }
        public int BytesSent { get; init; }
        public string StatusText { get; init; } = string.Empty;
    }
}
