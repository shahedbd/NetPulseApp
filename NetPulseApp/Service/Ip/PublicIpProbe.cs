using NetPulseApp.Helper;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// Resolves the public IPv4 and IPv6 addresses independently, each through
    /// its own fallback chain.
    /// </summary>
    /// <remarks>
    /// A geolocation provider reports only the address its request happened to
    /// arrive on, so on a dual-stack connection it returns one family and says
    /// nothing about the other. Each family is therefore probed against a
    /// family-pinned endpoint.
    /// <para>
    /// Because these results <b>override</b> the provider's address, a single
    /// endpoint here would be the weakest link in the whole service: one host
    /// being down or rate-limiting would make a working connection report
    /// "Not detected" for a family it actually has. Each family runs a chain
    /// (see <see cref="AppConfig.Ipv4ProbeEndpoints"/>), mirroring how the geo
    /// providers already fall back.
    /// </para>
    /// <para>
    /// A failed IPv6 chain remains the normal case on IPv4-only connections and
    /// is reported as "not detected", never as an error.
    /// </para>
    /// </remarks>
    public static class PublicIpProbe
    {
        /// <summary>
        /// Probes both families concurrently. Returns empty strings for
        /// whichever family is unavailable; never throws.
        /// </summary>
        public static async Task<(string Ipv4, string Ipv6)> ProbeAsync(
            HttpClient http, CancellationToken cancellationToken)
        {
            var v4 = ProbeFamilyAsync(http, AppConfig.Ipv4ProbeEndpoints, cancellationToken);
            var v6 = ProbeFamilyAsync(http, AppConfig.Ipv6ProbeEndpoints, cancellationToken);

            await Task.WhenAll(v4, v6);
            return (v4.Result, v6.Result);
        }

        /// <summary>
        /// Tries each endpoint in order until one returns a parseable address.
        /// </summary>
        private static async Task<string> ProbeFamilyAsync(
            HttpClient http, List<string> endpoints, CancellationToken cancellationToken)
        {
            if (endpoints == null) return string.Empty;

            foreach (var endpoint in endpoints)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string address = await FetchAsync(http, endpoint, cancellationToken);
                if (!string.IsNullOrEmpty(address)) return address;
            }

            return string.Empty;
        }

        private static async Task<string> FetchAsync(
            HttpClient http, string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(AppConfig.IpProbeTimeoutSeconds));

                var body = await http.GetStringAsync(endpoint, timeout.Token);

                // Validated rather than trusted: this value is rendered as the
                // user's address and copied to their clipboard. It also guards
                // against an endpoint returning an error page as 200 OK.
                return System.Net.IPAddress.TryParse(body.Trim(), out var ip)
                    ? ip.ToString()
                    : string.Empty;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The caller gave up — don't keep trying the rest of the chain.
                throw;
            }
            catch
            {
                // This endpoint timed out or failed; the chain moves on.
                return string.Empty;
            }
        }

        /// <summary>
        /// Reverse-DNS lookup for an address, resolved locally. Empty when the
        /// address has no PTR record, which is common and not an error.
        /// </summary>
        public static async Task<string> ReverseDnsAsync(string ip, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ip)) return string.Empty;

            try
            {
                var entry = await System.Net.Dns.GetHostEntryAsync(ip, cancellationToken);
                return entry.HostName == ip ? string.Empty : entry.HostName;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
