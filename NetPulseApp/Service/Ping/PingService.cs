// Service/Ping/PingService.cs
using NetPulseApp.Model;
using System.Net;
using System.Net.NetworkInformation;
using NetPing = System.Net.NetworkInformation.Ping;

namespace NetPulseApp.Service.Ping
{
    /// <summary>
    /// Runs async ICMP ping sequences and reports results via IProgress.
    /// All network work happens off the UI thread.
    /// </summary>
    public sealed class PingService
    {
        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Sends <paramref name="count"/> pings to <paramref name="host"/>.
        /// Reports each reply via <paramref name="progress"/> on the UI thread.
        /// Pass count = -1 for continuous mode (runs until cancelled).
        /// </summary>
        public async Task RunAsync(
            string host,
            int count,
            int packetBytes,
            int timeoutMs,
            bool resolveHostname,
            bool useIPv6,
            IProgress<PingResult> progress,
            CancellationToken ct)
        {
            string resolvedIp = await ResolveAsync(host, useIPv6, ct);

            bool continuous = count <= 0;
            int seq = 1;

            while (!ct.IsCancellationRequested && (continuous || seq <= count))
            {
                var result = await SendOneAsync(host, resolvedIp, seq,
                                                packetBytes, timeoutMs, ct);
                progress.Report(result);
                seq++;

                if (!ct.IsCancellationRequested && (continuous || seq <= count))
                    await Task.Delay(1000, ct).ConfigureAwait(false);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static async Task<string> ResolveAsync(
            string host, bool useIPv6, CancellationToken ct)
        {
            try
            {
                var entry = await Dns.GetHostEntryAsync(host, ct);
                var family = useIPv6
                    ? System.Net.Sockets.AddressFamily.InterNetworkV6
                    : System.Net.Sockets.AddressFamily.InterNetwork;
                var addr = entry.AddressList
                                      .FirstOrDefault(a => a.AddressFamily == family)
                               ?? entry.AddressList.FirstOrDefault();
                return addr?.ToString() ?? host;
            }
            catch
            {
                return host;
            }
        }

        private static async Task<PingResult> SendOneAsync(
            string host, string resolvedIp,
            int seq, int packetBytes, int timeoutMs,
            CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                using var pinger = new NetPing();
                byte[] buffer = new byte[packetBytes];
                var options = new PingOptions { DontFragment = true };

                PingReply reply = await pinger.SendPingAsync(
                    host, timeoutMs, buffer, options);

                bool ok = reply.Status == IPStatus.Success;
                return new PingResult
                {
                    Sequence = seq,
                    Host = host,
                    ResolvedIp = resolvedIp,
                    Success = ok,
                    RoundtripMs = ok ? reply.RoundtripTime : -1,
                    Ttl = ok ? reply.Options?.Ttl ?? 0 : 0,
                    BytesSent = packetBytes,
                    StatusText = ok ? "Reply" : reply.Status.ToString()
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return new PingResult
                {
                    Sequence = seq,
                    Host = host,
                    ResolvedIp = resolvedIp,
                    Success = false,
                    StatusText = ex.Message
                };
            }
        }
    }
}
