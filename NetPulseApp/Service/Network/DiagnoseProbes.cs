// Service/Network/DiagnoseProbes.cs
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DnsClient;

namespace NetPulseApp.Service.Network
{
    /// <summary>
    /// Single-shot network probes behind DiagnoseService. Every probe has its
    /// own short timeout and never throws for ordinary network failures —
    /// a failed probe is a result, not an exception.
    /// </summary>
    internal static class DiagnoseProbes
    {
        private const int PingTimeoutMs = 2000;
        private const int TcpTimeoutMs = 3000;
        private const int DnsTimeoutMs = 3000;
        private const int HttpTimeoutSec = 6;

        /// <summary>Captive-portal probe Windows itself uses (NCSI).</summary>
        public const string CaptiveProbeUrl = "http://www.msftconnecttest.com/connecttest.txt";
        public const string CaptiveProbeBody = "Microsoft Connect Test";

        private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(HttpTimeoutSec)
        };

        /// <summary>Sends up to <paramref name="tries"/> echoes; returns the best RTT, or null if none replied.</summary>
        public static async Task<long?> PingAsync(string host, int tries, CancellationToken token)
        {
            long? best = null;
            for (int i = 0; i < tries && !token.IsCancellationRequested; i++)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(host, PingTimeoutMs);
                    if (reply.Status == IPStatus.Success)
                        best = Math.Min(best ?? long.MaxValue, reply.RoundtripTime);
                }
                catch { /* unreachable / unresolvable — counts as no reply */ }
            }
            return best;
        }

        /// <summary>TCP connect latency — the ICMP-blocked fallback. Null on failure.</summary>
        public static async Task<long?> TcpConnectAsync(string host, int port, CancellationToken token)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TcpTimeoutMs);
                using var client = new TcpClient();
                var clock = Stopwatch.StartNew();
                await client.ConnectAsync(host, port, cts.Token);
                return clock.ElapsedMilliseconds;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Resolves via the system resolver. Returns (ok, ms, error).</summary>
        public static async Task<(bool Ok, long Ms, string Error)> ResolveSystemAsync(string name, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(DnsTimeoutMs);
                var addrs = await Dns.GetHostAddressesAsync(name, cts.Token);
                return (addrs.Length > 0, clock.ElapsedMilliseconds, addrs.Length > 0 ? "" : "no addresses");
            }
            catch (OperationCanceledException) { return (false, clock.ElapsedMilliseconds, "timed out"); }
            catch (SocketException ex) { return (false, clock.ElapsedMilliseconds, ex.SocketErrorCode.ToString()); }
            catch (Exception ex) { return (false, clock.ElapsedMilliseconds, ex.Message); }
        }

        /// <summary>Resolves via a specific public resolver (e.g. 1.1.1.1), bypassing the system DNS.</summary>
        public static async Task<(bool Ok, long Ms, string Error)> ResolveWithAsync(
            string server, string name, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                var client = new LookupClient(new LookupClientOptions(IPAddress.Parse(server))
                {
                    Timeout = TimeSpan.FromMilliseconds(DnsTimeoutMs),
                    Retries = 1,
                    UseCache = false
                });
                var response = await client.QueryAsync(name, QueryType.A, cancellationToken: token);
                bool ok = !response.HasError && response.Answers.ARecords().Any();
                return (ok, clock.ElapsedMilliseconds, ok ? "" : response.ErrorMessage);
            }
            catch (Exception ex) { return (false, clock.ElapsedMilliseconds, ex.Message); }
        }

        /// <summary>HTTPS GET; returns (ok, ms, status-or-error).</summary>
        public static async Task<(bool Ok, long Ms, string Detail)> HttpsAsync(string url, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            try
            {
                using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                int code = (int)response.StatusCode;
                return (code < 500, clock.ElapsedMilliseconds, $"HTTP {code}");
            }
            catch (HttpRequestException ex) when (ex.InnerException is System.Security.Authentication.AuthenticationException)
            {
                return (false, clock.ElapsedMilliseconds, "TLS certificate rejected");
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
                return (false, clock.ElapsedMilliseconds, "timed out");
            }
            catch (Exception ex) { return (false, clock.ElapsedMilliseconds, ex.Message); }
        }

        /// <summary>
        /// Captive-portal check: the NCSI URL returns a fixed body over plain
        /// HTTP. A redirect or a different body means something (a hotel /
        /// airport login page) is intercepting traffic. Null = probe failed.
        /// </summary>
        public static async Task<bool?> IsCaptivePortalAsync(CancellationToken token)
        {
            try
            {
                using var response = await Http.GetAsync(CaptiveProbeUrl, token);
                int code = (int)response.StatusCode;
                if (code is >= 300 and < 400) return true;
                string body = await response.Content.ReadAsStringAsync(token);
                return !body.Trim().StartsWith(CaptiveProbeBody, StringComparison.Ordinal);
            }
            catch
            {
                return null;
            }
        }
    }
}
