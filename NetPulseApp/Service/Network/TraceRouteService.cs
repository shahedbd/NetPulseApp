// Service/Network/TraceRouteService.cs
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetPulseApp.Service.Network
{
    /// <summary>One traced hop. RttMs null = every probe timed out.</summary>
    public class TraceHopResult
    {
        public int Hop { get; set; }
        public string Address { get; set; } = "";
        public string Hostname { get; set; } = "";
        public double? RttMs { get; set; }
        public int Replies { get; set; }
        public int Probes { get; set; }
        public double ElapsedSec { get; set; }

        /// <summary>True when this hop answered with Success — i.e. it is
        /// the destination itself, so the trace ends here.</summary>
        public bool IsDestination { get; set; }
    }

    /// <summary>
    /// Traceroute engine behind the Traceroute page. Classic TTL walk
    /// (tracert-style): for each TTL 1..maxHops send ProbesPerHop ICMP
    /// echoes with that TTL, take the answering router's address, and stop
    /// at the first Success reply (the destination). Events are marshalled
    /// back to the UI thread via the SynchronizationContext captured at
    /// Start — same contract as PingService.
    /// </summary>
    public class TraceRouteService : IDisposable
    {
        private const int ProbesPerHop = 3;
        private const int DnsTimeoutMs = 1500;

        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        /// <summary>Fires once per hop, timeouts included.</summary>
        public event Action<TraceHopResult> HopReceived;

        /// <summary>Fires when the run aborts (e.g. unknown host).</summary>
        public event Action<string> RunError;

        /// <summary>Fires when a run ends — destination reached, max hops
        /// exhausted, Stop(), or error.</summary>
        public event Action<bool> RunFinished;

        public bool IsRunning { get; private set; }

        /// <summary>Starts a run. No-op while running.</summary>
        public void Start(string host, int maxHops, int timeoutMs)
        {
            if (IsRunning || string.IsNullOrWhiteSpace(host))
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;

            _ = RunAsync(host.Trim(), maxHops, timeoutMs, _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(string host, int maxHops, int timeoutMs, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            bool reached = false;

            try
            {
                var buffer = new byte[32];

                for (int ttl = 1; ttl <= maxHops && !token.IsCancellationRequested; ttl++)
                {
                    var hop = await ProbeHopAsync(host, ttl, timeoutMs, buffer, clock);
                    Post(() => HopReceived?.Invoke(hop));

                    if (hop.IsDestination)
                    {
                        reached = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Post(() => RunError?.Invoke(ex.Message));
            }
            finally
            {
                IsRunning = false;
                Post(() => RunFinished?.Invoke(reached));
            }
        }

        private static async Task<TraceHopResult> ProbeHopAsync(
            string host, int ttl, int timeoutMs, byte[] buffer, Stopwatch clock)
        {
            var result = new TraceHopResult
            {
                Hop = ttl,
                Probes = ProbesPerHop,
                ElapsedSec = Math.Round(clock.Elapsed.TotalSeconds, 1)
            };

            var rtts = new List<double>();
            bool destination = false;

            // One Ping instance per attempt — a single instance refuses
            // concurrent calls (see PingService's note).
            for (int i = 0; i < ProbesPerHop; i++)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(host, timeoutMs, buffer,
                        new PingOptions(ttl, dontFragment: true));

                    if (reply.Status == IPStatus.Success)
                    {
                        // The echo reached the target itself — TTL walk done.
                        destination = true;
                        result.Address = reply.Address?.ToString() ?? result.Address;
                        rtts.Add(reply.RoundtripTime);
                    }
                    else if (reply.Status == IPStatus.TtlExpired && reply.Address != null)
                    {
                        // An intermediate router discarded the packet and
                        // told us who it is — that's this hop.
                        result.Address = reply.Address.ToString();
                        rtts.Add(reply.RoundtripTime);
                    }
                }
                catch (PingException)
                {
                    // Unresolvable host — rethrow as a plain message so the
                    // page can toast it instead of a raw socket dump.
                    throw new InvalidOperationException($"Unknown host: {host}");
                }
                catch (SocketException)
                {
                    throw new InvalidOperationException($"Unknown host: {host}");
                }
            }

            result.Replies = rtts.Count;
            result.RttMs = rtts.Count > 0 ? Math.Round(rtts.Average()) : null;
            result.IsDestination = destination;
            if (result.Address.Length > 0)
                result.Hostname = await ResolveHostAsync(result.Address) ?? "";

            return result;
        }

        /// <summary>Reverse DNS with a hard timeout — GetHostEntryAsync is
        /// not cancellable and can hang seconds on silent resolvers.</summary>
        private static async Task<string> ResolveHostAsync(string address)
        {
            try
            {
                var resolve = Dns.GetHostEntryAsync(address);
                if (await Task.WhenAny(resolve, Task.Delay(DnsTimeoutMs)) == resolve)
                    return resolve.Result.HostName;
            }
            catch
            {
                // No PTR record or resolver unreachable — not an error.
            }
            return null;
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
