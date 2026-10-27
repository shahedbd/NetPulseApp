using System.Text.Json;
using NetPulseApp.Model;

using NetPulseApp.Helper;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// ipwho.is — keyless, HTTPS, and the richest of the free options: city,
    /// coordinates, timezone, currency, calling code, ASN and ISP in one call.
    /// Primary provider (decision D1).
    /// </summary>
    /// <remarks>
    /// Nests network identity under "connection" and timezone under
    /// "timezone"; <see cref="JsonRead.Obj"/> falls back to the root when a
    /// nested object is missing, so a flattened response still parses.
    /// </remarks>
    public sealed class IpWhoIsProvider : IIpProvider
    {
        public string Name => "ipwho.is";

        public async Task<IpInfo> FetchAsync(HttpClient http, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await http.GetAsync(AppConfig.IpWhoIsEndpoint, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                // ipwho.is answers HTTP 200 with {"success":false} for rate
                // limits and bad input, so status code alone isn't enough.
                if (JsonRead.Bool(root, "success") == false) return null;

                string ip = JsonRead.Str(root, "ip");
                if (string.IsNullOrEmpty(ip)) return null;

                var connection = JsonRead.Obj(root, "connection");
                var timezone = JsonRead.Obj(root, "timezone");
                var flag = JsonRead.Obj(root, "flag");

                var info = new IpInfo
                {
                    City = JsonRead.Str(root, "city"),
                    Region = JsonRead.Str(root, "region"),
                    Country = JsonRead.Str(root, "country", "country_name"),
                    CountryCode = JsonRead.Str(root, "country_code"),
                    Postal = JsonRead.Str(root, "postal"),
                    Continent = JsonRead.Str(root, "continent"),
                    Latitude = JsonRead.Num(root, "latitude"),
                    Longitude = JsonRead.Num(root, "longitude"),
                    CallingCode = JsonRead.Str(root, "calling_code"),

                    TimezoneId = JsonRead.Str(timezone, "id", "timezone"),
                    UtcOffset = JsonRead.Str(timezone, "utc"),

                    Isp = JsonRead.Str(connection, "isp"),
                    Organization = JsonRead.Str(connection, "org"),
                    Asn = FormatAsn(JsonRead.Str(connection, "asn")),
                    AsnName = JsonRead.Str(connection, "org", "domain"),

                    ProviderName = Name,
                    RetrievedUtc = DateTime.UtcNow
                };

                AssignAddress(info, ip);

                // Currency is an object on this provider; the code is what the
                // Location page shows.
                var currency = JsonRead.Obj(root, "currency");
                info.Currency = JsonRead.Str(currency, "code", "currency");

                // Not a documented field on the free tier — read it if present,
                // otherwise leave unknown rather than guessing (decision D2).
                info.IsHosting = JsonRead.Bool(root, "is_hosting", "hosting");

                _ = flag; // Flag imagery is unused; the UI renders the country code.
                return info;
            }
            catch
            {
                // Ordinary network failure — the chain moves to the next provider.
                return null;
            }
        }

        /// <summary>Normalises 58715 / "AS58715" to a single "AS58715" form.</summary>
        internal static string FormatAsn(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            raw = raw.Trim();
            return raw.StartsWith("AS", StringComparison.OrdinalIgnoreCase) ? raw : "AS" + raw;
        }

        /// <summary>
        /// Files the returned address into the right slot. Providers report
        /// whichever family the request happened to use, so this is decided by
        /// parsing rather than assumed to be IPv4.
        /// </summary>
        internal static void AssignAddress(IpInfo info, string ip)
        {
            if (!System.Net.IPAddress.TryParse(ip, out var parsed)) return;

            if (parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                info.PublicIpv6 = parsed.ToString();
            else
                info.PublicIpv4 = parsed.ToString();
        }
    }
}
