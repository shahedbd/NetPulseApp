using System.Text.Json;
using NetPulseApp.Model;

using NetPulseApp.Helper;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// ipapi.co — keyless, HTTPS, flat JSON. Secondary provider (decision D1).
    /// Covers the same ground as ipwho.is with different field names and adds
    /// currency name and languages.
    /// </summary>
    public sealed class IpApiCoProvider : IIpProvider
    {
        public string Name => "ipapi.co";

        public async Task<IpInfo> FetchAsync(HttpClient http, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await http.GetAsync(AppConfig.IpApiCoEndpoint, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                // Rate limiting comes back as {"error":true,"reason":"..."}
                // with a 200, so the status code alone isn't enough.
                if (JsonRead.Bool(root, "error") == true) return null;

                string ip = JsonRead.Str(root, "ip");
                if (string.IsNullOrEmpty(ip)) return null;

                var info = new IpInfo
                {
                    City = JsonRead.Str(root, "city"),
                    Region = JsonRead.Str(root, "region"),
                    Country = JsonRead.Str(root, "country_name", "country"),
                    CountryCode = JsonRead.Str(root, "country_code", "country"),
                    Postal = JsonRead.Str(root, "postal"),
                    Continent = JsonRead.Str(root, "continent_code"),
                    Latitude = JsonRead.Num(root, "latitude"),
                    Longitude = JsonRead.Num(root, "longitude"),

                    TimezoneId = JsonRead.Str(root, "timezone"),
                    UtcOffset = JsonRead.Str(root, "utc_offset"),

                    Currency = JsonRead.Str(root, "currency"),
                    CallingCode = JsonRead.Str(root, "country_calling_code"),
                    Languages = JsonRead.Str(root, "languages"),

                    Isp = JsonRead.Str(root, "org"),
                    Organization = JsonRead.Str(root, "org"),
                    Asn = IpWhoIsProvider.FormatAsn(JsonRead.Str(root, "asn")),
                    AsnName = JsonRead.Str(root, "org"),

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
    }
}
