using NetPulseApp.Model;

using NetPulseApp.Helper;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// Cloudflare's <c>/cdn-cgi/trace</c> — last resort in the chain
    /// (decision D1). Returns only the IP and a two-letter country, but it is
    /// keyless, effectively never rate-limited, and reachable when the
    /// geolocation APIs are blocked or down.
    /// </summary>
    /// <remarks>
    /// The response is plain <c>key=value</c> lines, not JSON. The
    /// <c>colo</c> field names the Cloudflare edge that served the request —
    /// the Speed Test page reports it as the test server, since the Cloudflare
    /// speed backend has no server picker (decision D5).
    /// </remarks>
    public sealed class CloudflareTraceProvider : IIpProvider
    {
        public string Name => "Cloudflare";

        public async Task<IpInfo> FetchAsync(HttpClient http, CancellationToken cancellationToken)
        {
            try
            {
                var body = await http.GetStringAsync(AppConfig.CloudflareTraceEndpoint, cancellationToken);
                var fields = Parse(body);

                if (!fields.TryGetValue("ip", out var ip) || string.IsNullOrEmpty(ip))
                    return null;

                var info = new IpInfo
                {
                    CountryCode = fields.GetValueOrDefault("loc", string.Empty),
                    ProviderName = Name,
                    RetrievedUtc = DateTime.UtcNow
                };

                IpWhoIsProvider.AssignAddress(info, ip);
                return info;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Reads the trace body into a dictionary. Also used by the Speed Test
        /// page to read <c>colo</c>.
        /// </summary>
        public static Dictionary<string, string> Parse(string body)
        {
            var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(body)) return fields;

            foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;

                fields[line[..eq].Trim()] = line[(eq + 1)..].Trim();
            }

            return fields;
        }

        /// <summary>
        /// Fetches just the edge location code (e.g. "DAC"), for the Speed Test
        /// page's server label. Empty when unavailable.
        /// </summary>
        public static async Task<string> GetColoAsync(HttpClient http, CancellationToken cancellationToken)
        {
            try
            {
                var body = await http.GetStringAsync(AppConfig.CloudflareTraceEndpoint, cancellationToken);
                return Parse(body).GetValueOrDefault("colo", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
