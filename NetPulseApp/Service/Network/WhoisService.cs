// Service/Network/WhoisService.cs
using System.Net;
using System.Text.Json;

namespace NetPulseApp.Service.Network
{
    /// <summary>One WHOIS fact, ready for the FIELD / VALUE table.</summary>
    public class WhoisRecord
    {
        public string Field { get; set; } = "";
        public string Value { get; set; } = "";
    }

    /// <summary>
    /// WHOIS engine behind the WHOIS IP page. Queries RDAP (the registry's
    /// structured-JSON WHOIS successor) via the rdap.org bootstrap — one
    /// built-in HttpClient call, no WHOIS-client dependency. IPs resolve
    /// to network registration data, domains to registry data. Events are
    /// marshalled back to the UI thread via the SynchronizationContext
    /// captured at Start — same contract as the other tool services.
    /// </summary>
    public class WhoisService : IDisposable
    {
        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        /// <summary>Fires once per extracted fact, in display order.</summary>
        public event Action<WhoisRecord> RecordReceived;

        /// <summary>Fires when the lookup aborts (not found, network).</summary>
        public event Action<string> RunError;

        /// <summary>Fires when the run ends — data found, error, or Stop().</summary>
        public event Action<bool> RunFinished;

        public bool IsRunning { get; private set; }

        /// <summary>Starts a lookup. Detects IP vs domain automatically.</summary>
        public void Start(string target)
        {
            if (IsRunning || string.IsNullOrWhiteSpace(target))
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;

            _ = RunAsync(target.Trim(), _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(string target, CancellationToken token)
        {
            bool any = false;
            try
            {
                bool isIp = IPAddress.TryParse(target, out _);
                string kind = isIp ? "ip" : "domain";

                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://rdap.org/{kind}/{Uri.EscapeDataString(target)}");
                request.Headers.Add("User-Agent", "NetPulseApp/1.0 (RDAP client)");

                using var response = await Http.SendAsync(request, token);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    throw new InvalidOperationException(
                        $"No registration data found for {target}.");
                response.EnsureSuccessStatusCode();

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
                foreach (var record in Flatten(doc.RootElement))
                {
                    any = true;
                    var captured = record;
                    Post(() => RecordReceived?.Invoke(captured));
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() — keep whatever rows already arrived.
            }
            catch (Exception ex)
            {
                // Not-found (thrown above), DNS, TLS, timeout — all surface
                // once with the message the page toasts.
                Post(() => RunError?.Invoke(ex.Message));
            }
            finally
            {
                IsRunning = false;
                Post(() => RunFinished?.Invoke(any));
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // RDAP JSON → FIELD / VALUE rows
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Picks the interesting bits out of an RDAP object in
        /// display order. JsonDocument key-walking keeps this a few dozen
        /// lines instead of a mirror of the whole RDAP schema.</summary>
        private static IEnumerable<WhoisRecord> Flatten(JsonElement root)
        {
            bool isNetwork = Str(root, "objectClassName") == "ip network";
            yield return new WhoisRecord { Field = "Object type",
                Value = isNetwork ? "IP network" : "Domain" };

            foreach (var (name, label) in new[]
            {
                ("handle", "Handle"),
                (isNetwork ? "name" : "ldhName", "Name")
            })
            {
                string value = Str(root, name);
                if (value.Length > 0)
                    yield return new WhoisRecord { Field = label, Value = value };
            }

            if (isNetwork)
            {
                string range = $"{Str(root, "startAddress")} – {Str(root, "endAddress")}".Trim(' ', '–');
                if (range.Length > 0)
                    yield return new WhoisRecord { Field = "Range", Value = range };

                foreach (var (name, label) in new[]
                {
                    ("type", "Network type"),
                    ("country", "Country")
                })
                {
                    string value = Str(root, name);
                    if (value.Length > 0)
                        yield return new WhoisRecord { Field = label, Value = value };
                }
            }

            foreach (var e in Arr(root, "events"))
            {
                string action = Str(e, "eventAction");
                string label = action switch
                {
                    "registration" => "Registered",
                    "expiration" => "Expires",
                    "last changed" or "last update of RDAP database" => "Last changed",
                    _ => null
                };
                if (label == null) continue;
                if (DateTime.TryParse(Str(e, "eventDate"), out var date))
                    yield return new WhoisRecord { Field = label, Value = date.ToString("dd MMM yyyy") };
            }

            if (root.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.Array)
            {
                string joined = string.Join(", ", status.EnumerateArray()
                    .Where(s => s.ValueKind == JsonValueKind.String)
                    .Select(s => s.GetString()));
                if (joined.Length > 0)
                    yield return new WhoisRecord { Field = "Status", Value = joined };
            }

            foreach (var record in EntityRows(root))
                yield return record;

            var ns = Arr(root, "nameservers")
                .Select(n => Str(n, "ldhName")).Where(s => s.Length > 0).ToList();
            if (ns.Count > 0)
                yield return new WhoisRecord { Field = "Name servers", Value = string.Join(", ", ns) };
        }

        /// <summary>Registrant-ish contact facts from entities[] vCards.</summary>
        private static IEnumerable<WhoisRecord> EntityRows(JsonElement root)
        {
            foreach (var entity in Arr(root, "entities"))
            {
                string role = Arr(entity, "roles")
                    .Select(r => r.ValueKind == JsonValueKind.String ? r.GetString() : null)
                    .FirstOrDefault(r => r is "registrant" or "administrative" or "technical") ?? "";
                if (role.Length == 0) continue;

                string prefix = role switch { "registrant" => "Registrant", _ => "Contact" };
                // vcardArray = ["vcard", [prop, prop, …]] — skip the label,
                // then walk the property arrays.
                foreach (var vcard in Arr(entity, "vcardArray").Skip(1)
                             .Where(v => v.ValueKind == JsonValueKind.Array)
                             .SelectMany(v => v.EnumerateArray()).Where(IsVcardProp))
                {
                    // Each prop is ["fn"|"org"|"email"|"adr", params, type, value].
                    var parts = vcard.EnumerateArray().ToList();
                    if (parts.Count < 4) continue;
                    string name = parts[0].GetString();
                    string value = parts[3].ValueKind == JsonValueKind.Array
                        ? string.Join(", ", parts[3].EnumerateArray()
                            .Where(p => p.ValueKind == JsonValueKind.String)
                            .Select(p => p.GetString()).Where(s => s?.Length > 0))
                        : parts[3].GetString();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    string label = name switch
                    {
                        "fn" => $"{prefix} name",
                        "org" => $"{prefix} organization",
                        "email" => $"{prefix} email",
                        "adr" => $"{prefix} address",
                        _ => null
                    };
                    if (label != null)
                        yield return new WhoisRecord { Field = label, Value = value };
                }
            }
        }

        private static bool IsVcardProp(JsonElement e) =>
            e.ValueKind == JsonValueKind.Array && e.GetArrayLength() > 0;

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() : "";

        private static List<JsonElement> Arr(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array
                ? v.EnumerateArray().ToList() : new List<JsonElement>();

        /// <summary>Marshals event raises to the UI thread if Start was called there.</summary>
        private void Post(Action raise)
        {
            if (_ui != null)
                _ui.Post(_ => raise(), null);
            else
                raise();
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
