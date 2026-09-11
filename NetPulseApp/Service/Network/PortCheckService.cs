// Service/Network/PortCheckService.cs
using System.Diagnostics;
using System.Net.Sockets;

namespace NetPulseApp.Service.Network
{
    /// <summary>One port probe. Status: Open / Closed / Filtered.</summary>
    public class PortCheckResult
    {
        public int Port { get; set; }
        public string Status { get; set; } = "Filtered";
        public int ElapsedMs { get; set; }
    }

    /// <summary>
    /// Port-check engine behind the Port Checker page. Probes every port
    /// in parallel with a plain TCP connect (no raw sockets needed):
    /// connected = Open, actively refused = Closed, timeout or unreachable
    /// = Filtered. Events are marshalled back to the UI thread via the
    /// SynchronizationContext captured at Start — same contract as the
    /// other tool services.
    /// </summary>
    public class PortCheckService : IDisposable
    {
        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        /// <summary>Fires once per probed port, as each probe settles.</summary>
        public event Action<PortCheckResult> PortChecked;

        /// <summary>Fires when the run aborts (e.g. unresolvable host).</summary>
        public event Action<string> RunError;

        /// <summary>Fires when the run ends — all probes settled, Stop(),
        /// or fatal error (unresolvable host).</summary>
        public event Action<bool> RunFinished;

        public bool IsRunning { get; private set; }

        /// <summary>Starts a run. No-op while running.</summary>
        public void Start(string host, IReadOnlyList<int> ports, int timeoutMs)
        {
            if (IsRunning || string.IsNullOrWhiteSpace(host) || ports.Count == 0)
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;

            _ = RunAsync(host.Trim(), ports, timeoutMs, _cts.Token);
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(string host, IReadOnlyList<int> ports, int timeoutMs, CancellationToken token)
        {
            bool any = false;
            try
            {
                // Probes run in parallel — a 10-port list at 3s worst case
                // still finishes in ~3s, not 30s.
                var tasks = ports.Select(port => ProbeAsync(host, port, timeoutMs, token)).ToArray();
                var results = await Task.WhenAll(tasks);
                foreach (var result in results)
                {
                    if (result == null) continue; // cancelled before settling
                    any = true;
                    var captured = result;
                    Post(() => PortChecked?.Invoke(captured));
                }
            }
            catch (OperationCanceledException)
            {
                // Stop() — rows that already settled were posted.
            }
            catch (Exception ex)
            {
                // e.g. unresolvable host — surfaces once, not per port.
                Post(() => RunError?.Invoke(ex.Message));
            }
            finally
            {
                IsRunning = false;
                Post(() => RunFinished?.Invoke(any));
            }
        }

        /// <summary>Null when cancelled before the probe settled.</summary>
        private static async Task<PortCheckResult> ProbeAsync(
            string host, int port, int timeoutMs, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            var result = new PortCheckResult { Port = port };

            try
            {
                using var client = new TcpClient();
                // ConnectAsync returns a ValueTask here, so timeout via a
                // linked token (CancelAfter) instead of Task.WhenAny.
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeoutCts.CancelAfter(timeoutMs);

                await client.ConnectAsync(host, port, timeoutCts.Token);
                result.Status = "Open";
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                // Our own timeout fired — no answer at all, firewalled or
                // silently dropped. (Stop() flows through the linked token
                // with token itself cancelled and lands in the next catch.)
                result.Status = "Filtered";
            }
            catch (OperationCanceledException)
            {
                return null; // Stop() — no stale row
            }
            catch (SocketException ex)
            {
                // An explicit refusal is a real answer: nothing listening.
                // Everything else (unreachable, no route…) reads as Filtered.
                result.Status = ex.SocketErrorCode == SocketError.ConnectionRefused
                    ? "Closed"
                    : "Filtered";
            }
            catch
            {
                result.Status = "Filtered";
            }

            clock.Stop();
            result.ElapsedMs = (int)clock.ElapsedMilliseconds;
            return result;
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
