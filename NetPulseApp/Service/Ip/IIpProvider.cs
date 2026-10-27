using NetPulseApp.Model;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// One external source of public-IP and geolocation data.
    /// </summary>
    /// <remarks>
    /// Implementations must be keyless. Shipping an API key inside an MSIX
    /// makes it extractable by anyone who unzips the package, and ip-api.com's
    /// free tier is HTTP-only and licensed for non-commercial use — neither is
    /// acceptable for a Store app. See decision D1 in the development plan.
    /// </remarks>
    public interface IIpProvider
    {
        /// <summary>Shown in Settings and recorded on each result.</summary>
        string Name { get; }

        /// <summary>
        /// Fetches what this provider knows. Returns null when the provider
        /// fails or answers with something unusable, so the chain can move on.
        /// Implementations must not throw for ordinary network failures.
        /// </summary>
        Task<IpInfo> FetchAsync(HttpClient http, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Shared helpers for reading loosely-typed provider JSON.
    /// </summary>
    /// <remarks>
    /// Providers disagree on field names for the same concept
    /// (country_name vs. country, asn vs. connection.asn), and free endpoints
    /// change shape without notice. Every read is therefore by-name-with-
    /// alternatives and tolerant of the field being absent or the wrong type —
    /// a missing field degrades one row, never the whole page.
    /// </remarks>
    internal static class JsonRead
    {
        internal static string Str(System.Text.Json.JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (!root.TryGetProperty(name, out var el)) continue;

                switch (el.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.String:
                        var s = el.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
                        break;
                    case System.Text.Json.JsonValueKind.Number:
                        return el.ToString();
                    case System.Text.Json.JsonValueKind.True:
                    case System.Text.Json.JsonValueKind.False:
                        return el.GetBoolean().ToString();
                }
            }
            return string.Empty;
        }

        internal static double? Num(System.Text.Json.JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (!root.TryGetProperty(name, out var el)) continue;

                if (el.ValueKind == System.Text.Json.JsonValueKind.Number && el.TryGetDouble(out var d))
                    return d;

                // Some providers return coordinates as strings.
                if (el.ValueKind == System.Text.Json.JsonValueKind.String &&
                    double.TryParse(el.GetString(), System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
            }
            return null;
        }

        internal static bool? Bool(System.Text.Json.JsonElement root, params string[] names)
        {
            foreach (var name in names)
            {
                if (!root.TryGetProperty(name, out var el)) continue;

                if (el.ValueKind == System.Text.Json.JsonValueKind.True) return true;
                if (el.ValueKind == System.Text.Json.JsonValueKind.False) return false;
            }
            return null;
        }

        /// <summary>Reads a nested object, returning the root itself if absent.</summary>
        internal static System.Text.Json.JsonElement Obj(System.Text.Json.JsonElement root, string name)
        {
            if (root.TryGetProperty(name, out var el) &&
                el.ValueKind == System.Text.Json.JsonValueKind.Object)
                return el;

            return root;
        }
    }
}
