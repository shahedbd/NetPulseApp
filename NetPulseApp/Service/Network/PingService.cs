// Service/Network/PingService.cs
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace NetPulseApp.Service.Network
{
    /// <summary>One ping attempt. RoundtripMs null = no reply (timeout/error).</summary>
    public class PingResult
    {
        public int Seq { get; set; }
        public double ElapsedSec { get; set; }
        public long? RoundtripMs { get; set; }
        public int? Ttl { get; set; }
        public string Status { get; set; } = "Error";
        public string Address { get; set; } = "";
    }

    /// <summary>Running totals for the current ping run.</summary>
    public class PingStats
    {
        public int Sent { get; set; }
        public int Received { get; set; }
        public long Min { get; set; } = long.MaxValue;
        public long Max { get; set; }
        public double Sum { get; set; }

        public int Lost => Sent - Received;
        public double LossPercent => Sent == 0 ? 0 : 100.0 * Lost / Sent;
        public double Avg => Received == 0 ? 0 : Sum / Received;

        /// <summary>"12/13.6/15 ms" summary text, "—" before the first reply.</summary>
        public string MinAvgMaxText =>
            Received == 0 ? "—" : $"{Min}/{Avg:0.#}/{Max} ms";
    }

    /// <summary>
    /// Ping engine behind the Ping page. Runs a background loop that sends
    /// one ICMP echo per interval (continuous, or a fixed count), tracks
    /// stats, and raises events marshalled back to the UI thread via the
    /// SynchronizationContext captured at Start.
    /// </summary>
    public class PingService : IDisposable
    {
        private const int TimeoutMs = 4000;

        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        /// <summary>Fires once per attempt, timeout or error included.</summary>
        public event Action<PingResult, PingStats> ReplyReceived;

        /// <summary>Fires when the run aborts (e.g. unexpected failure).</summary>
        public event Action<string> RunError;

        /// <summary>Fires when a run ends — count reached, Stop(), or error.</summary>
        public event Action RunFinished;

        public bool IsRunning { get; private set; }

        /// <summary>Starts a run. count 0 = continuous until Stop(). No-op while running.</summary>
        public void Start(string host, int intervalMs, int count)
        {
            if (IsRunning || string.IsNullOrWhiteSpace(host))
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;

            _ = RunAsync(host.Trim(), intervalMs, count, _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(string host, int intervalMs, int count, CancellationToken token)
        {
            var stats = new PingStats();
            var clock = Stopwatch.StartNew();
            int seq = 0;

            try
            {
                while (!token.IsCancellationRequested && (count == 0 || seq < count))
                {
                    seq++;
                    stats.Sent++;

                    var result = await PingOnceAsync(host, seq, clock, stats);
                    Post(() => ReplyReceived?.Invoke(result, stats));

                    bool more = count == 0 || seq < count;
                    if (!more)
                        break;

                    try { await Task.Delay(intervalMs, token); }
                    catch (TaskCanceledException) { break; }
                }
            }
            catch (Exception ex)
            {
                Post(() => RunError?.Invoke(ex.Message));
            }
            finally
            {
                IsRunning = false;
                Post(() => RunFinished?.Invoke());
            }
        }

        private static async Task<PingResult> PingOnceAsync(
            string host, int seq, Stopwatch clock, PingStats stats)
        {
            var result = new PingResult
            {
                Seq = seq,
                ElapsedSec = Math.Round(clock.Elapsed.TotalSeconds, 1)
            };

            try
            {
                // One Ping instance per attempt — a single instance refuses
                // concurrent calls and SendPingAsync isn't cancellable, so a
                // fresh one keeps Stop() simple (worst case: one in-flight
                // request finishes/times out, then the loop exits).
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, TimeoutMs);

                result.Address = reply.Address?.ToString() ?? "";
                result.Ttl = reply.Options?.Ttl;

                if (reply.Status == IPStatus.Success)
                {
                    stats.Received++;
                    result.RoundtripMs = reply.RoundtripTime;
                    result.Status = "Reply";
                    stats.Sum += reply.RoundtripTime;
                    stats.Min = Math.Min(stats.Min, reply.RoundtripTime);
                    stats.Max = Math.Max(stats.Max, reply.RoundtripTime);
                }
                else
                {
                    result.Status = FriendlyStatus(reply.Status);
                }
            }
            catch (PingException ex)
            {
                // Typically DNS resolution failure — SendPingAsync throws
                // instead of returning a status for unresolvable hosts.
                bool hostNotFound = ex.InnerException is SocketException se
                    && se.SocketErrorCode == SocketError.HostNotFound;
                result.Status = hostNotFound ? "Unknown host" : "Error";
            }
            catch
            {
                result.Status = "Error";
            }

            return result;
        }

        private static string FriendlyStatus(IPStatus status) => status switch
        {
            IPStatus.TimedOut => "Timeout",
            IPStatus.DestinationHostUnreachable => "Unreachable",
            IPStatus.TtlExpired => "TTL expired",
            IPStatus.TimeExceeded => "Time exceeded",
            _ => status.ToString()
        };

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
