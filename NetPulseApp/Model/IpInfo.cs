namespace NetPulseApp.Model
{
    /// <summary>
    /// Everything the app knows about the current public IP, normalised across
    /// providers so pages never care which one answered.
    /// </summary>
    /// <remarks>
    /// Every field is nullable-by-convention: providers differ in what their
    /// free tiers return, and a missing field is normal, not an error. Pages
    /// render "—" for anything empty
    /// rather than "N/A" — see the anti-N/A rule in the requirements.
    /// </remarks>
    public sealed class IpInfo
    {
        // ── Addresses ────────────────────────────────────────────────────
        public string PublicIpv4 { get; set; } = string.Empty;
        public string PublicIpv6 { get; set; } = string.Empty;

        /// <summary>Reverse-DNS name, resolved locally rather than by a provider.</summary>
        public string Hostname { get; set; } = string.Empty;

        // ── Location (approximate — derived from the IP, never GPS) ───────
        public string City { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string Postal { get; set; } = string.Empty;
        public string Continent { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>IANA zone id, e.g. "Asia/Dhaka".</summary>
        public string TimezoneId { get; set; } = string.Empty;
        public string UtcOffset { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;
        public string CallingCode { get; set; } = string.Empty;
        public string Languages { get; set; } = string.Empty;

        // ── Network identity ─────────────────────────────────────────────
        public string Isp { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Asn { get; set; } = string.Empty;
        public string AsnName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the address belongs to a hosting/datacenter range, when the
        /// provider says so. Deliberately not a Residential/Business/Mobile
        /// classification: that is paid-tier data on every provider, and
        /// guessing it would be worse than leaving it unknown.
        /// </summary>
        public bool? IsHosting { get; set; }

        // ── Provenance ───────────────────────────────────────────────────
        /// <summary>Which provider answered, for the Settings/About surface.</summary>
        public string ProviderName { get; set; } = string.Empty;

        public DateTime RetrievedUtc { get; set; }

        /// <summary>
        /// True when this came from disk rather than the network. The UI must
        /// label cached values with their age — a stale IP shown as live is
        /// worse than showing nothing.
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>True when no provider returned anything usable.</summary>
        public bool IsEmpty => string.IsNullOrEmpty(PublicIpv4) && string.IsNullOrEmpty(PublicIpv6);

        /// <summary>Best available single address for hero display.</summary>
        public string PrimaryIp(bool preferIpv4 = true)
        {
            if (preferIpv4)
                return !string.IsNullOrEmpty(PublicIpv4) ? PublicIpv4 : PublicIpv6;

            return !string.IsNullOrEmpty(PublicIpv6) ? PublicIpv6 : PublicIpv4;
        }

        /// <summary>"City, Country" — collapses cleanly when parts are missing.</summary>
        public string LocationLine()
        {
            var parts = new[] { City, Region, Country }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToArray();

            return parts.Length == 0 ? string.Empty : string.Join(", ", parts);
        }

        /// <summary>Current wall-clock time at the IP's timezone, if known.</summary>
        public DateTime? LocalTimeAtIp()
        {
            if (string.IsNullOrWhiteSpace(TimezoneId)) return null;

            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(TimezoneId);
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            }
            catch
            {
                // Windows ships IANA support from .NET 6 on, but an unknown or
                // newly-added zone id is still possible — not worth surfacing.
                return null;
            }
        }
    }
}
