// Service/Network/DnsLookupService.cs
using System.Net;
using DnsClient;
using DnsClient.Protocol;

namespace NetPulseApp.Service.Network
{
    /// <summary>One resolved DNS record, ready for the results table.</summary>
    public class DnsRecordResult
    {
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
        public string Ttl { get; set; } = "";
        public string Extra { get; set; } = "";
    }

    /// <summary>
    /// DNS engine behind the DNS Lookup page. Runs one DnsClient query per
    /// record type (or the whole common set for "All") on a background
    /// task and raises events marshalled to the UI thread via the
    /// SynchronizationContext captured at Start — same contract as
    /// PingService / TraceRouteService.
    /// </summary>
    public class DnsLookupService : IDisposable
    {
        private static readonly QueryType[] CommonTypes =
            { QueryType.A, QueryType.AAAA, QueryType.CNAME, QueryType.MX, QueryType.TXT, QueryType.NS, QueryType.SOA };

        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        /// <summary>Fires once per resolved record, in query order.</summary>
        public event Action<DnsRecordResult> RecordReceived;

        /// <summary>Fires when the whole lookup ends — found something,
        /// nothing, error, or Stop().</summary>
        public event Action<bool> RunFinished;

        public bool IsRunning { get; private set; }

        /// <summary>Starts a lookup. queryType null = all common types.
        /// No-op while running.</summary>
        public void Start(string domain, QueryType? queryType, string dnsServer)
        {
            if (IsRunning || string.IsNullOrWhiteSpace(domain))
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;

            _ = RunAsync(domain.Trim(), queryType, dnsServer?.Trim(), _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(string domain, QueryType? queryType, string dnsServer, CancellationToken token)
        {
            bool any = false;
            try
            {
                var client = BuildClient(dnsServer);
                var types = queryType.HasValue ? new[] { queryType.Value } : CommonTypes;

                foreach (var type in types)
                {
                    if (token.IsCancellationRequested)
                        break;

                    // One query per type — merged result rows arrive as they
                    // resolve, so the table fills progressively.
                    var response = await client.QueryAsync(domain, type, cancellationToken: token);
                    foreach (var record in FormatRecords(response))
                    {
                        any = true;
                        var captured = record;
                        Post(() => RecordReceived?.Invoke(captured));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() — keep whatever rows already arrived.
            }
            catch (DnsResponseException ex)
            {
                // NXDOMAIN and friends surface with a response code.
                Post(() => RecordReceived?.Invoke(new DnsRecordResult
                {
                    Type = "Error",
                    Value = ex.Code.ToString(),
                    Ttl = "—",
                    Extra = ex.DnsError ?? "DNS query failed."
                }));
            }
            catch (Exception ex)
            {
                Post(() => RecordReceived?.Invoke(new DnsRecordResult
                {
                    Type = "Error",
                    Value = "Error",
                    Ttl = "—",
                    Extra = ex.Message
                }));
            }
            finally
            {
                IsRunning = false;
                Post(() => RunFinished?.Invoke(any));
            }
        }

        /// <summary>System resolver by default, or a custom one (e.g.
        /// 8.8.8.8) with a short timeout so dead servers fail fast.</summary>
        private static LookupClient BuildClient(string dnsServer)
        {
            if (string.IsNullOrEmpty(dnsServer) || dnsServer == "Default")
                return new LookupClient();

            if (IPAddress.TryParse(dnsServer, out var ip))
                return new LookupClient(new LookupClientOptions(ip)
                {
                    Timeout = TimeSpan.FromSeconds(3),
                    Retries = 1
                });

            return new LookupClient();
        }

        /// <summary>Flattens a query response into table rows. Pattern
        /// matches the concrete record classes — DnsClient 1.8 exposes no
        /// per-type interfaces.</summary>
        private static IEnumerable<DnsRecordResult> FormatRecords(IDnsQueryResponse response)
        {
            foreach (var answer in response.Answers)
            {
                var record = new DnsRecordResult
                {
                    Type = answer.RecordType.ToString().ToUpperInvariant(),
                    Ttl = $"{answer.TimeToLive}"
                };

                switch (answer)
                {
                    case AddressRecord a: // ARecord and AaaaRecord both
                        record.Value = a.Address.ToString();
                        record.Extra = a.DomainName.Value;
                        break;
                    case CNameRecord c:
                        record.Value = c.CanonicalName.Value;
                        break;
                    case MxRecord mx:
                        record.Value = mx.Exchange.Value;
                        record.Extra = $"Preference {mx.Preference}";
                        break;
                    case TxtRecord txt:
                        record.Value = string.Join(" ", txt.EscapedText);
                        break;
                    case NsRecord ns:
                        record.Value = ns.NSDName.Value;
                        break;
                    case SoaRecord soa:
                        record.Value = soa.MName.Value;
                        record.Extra = $"Serial {soa.Serial} · Refresh {soa.Refresh}";
                        break;
                    case PtrRecord ptr:
                        record.Value = ptr.PtrDomainName.Value;
                        break;
                    default:
                        record.Value = answer.ToString();
                        break;
                }

                yield return record;
            }
        }

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
