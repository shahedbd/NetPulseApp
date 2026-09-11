// Service/Network/DashboardService.cs
using System.Net.NetworkInformation;

namespace NetPulseApp.Service.Network
{
    /// <summary>One interface snapshot, ready for the Dashboard tiles and
    /// connection card.</summary>
    public class DashboardSnapshot
    {
        public string InterfaceName { get; set; } = "—";
        public string InterfaceType { get; set; } = "—";
        public string IpAddress { get; set; } = "—";
        public string Gateway { get; set; } = "—";
        public string DnsServer { get; set; } = "—";
        public string MacAddress { get; set; } = "—";
        public string Status { get; set; } = "—";

        /// <summary>Live throughput since the previous snapshot (interface
        /// counters delta — actual current traffic, not a speed test).</summary>
        public double DownloadMbps { get; set; }
        public double UploadMbps { get; set; }
    }

    /// <summary>
    /// Monitor engine behind the Dashboard page. Polls the active network
    /// interface once a second (addresses, gateway, DNS, MAC) and derives
    /// live download/upload rates from the interface counter deltas. Events
    /// are marshalled to the UI thread via the SynchronizationContext
    /// captured at Start — same contract as the other tool services.
    /// </summary>
    public class DashboardService : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly SynchronizationContext _ui;
        private long _lastRx = -1;
        private long _lastTx = -1;
        private DateTime _lastTick;

        /// <summary>Fires once per polling interval with fresh data.</summary>
        public event Action<DashboardSnapshot> Updated;

        public bool IsRunning { get; private set; }

        public DashboardService()
        {
            _ui = SynchronizationContext.Current;
        }

        public void Start()
        {
            if (IsRunning)
                return;
            IsRunning = true;
            _ = RunAsync(_cts.Token);
        }

        public void Stop() => _cts.Cancel();

        private async Task RunAsync(CancellationToken token)
        {
            _lastTick = DateTime.UtcNow;

            while (!token.IsCancellationRequested)
            {
                var snapshot = Snapshot();
                var captured = snapshot;
                Post(() => Updated?.Invoke(captured));

                try { await Task.Delay(1000, token); }
                catch (TaskCanceledException) { break; }
            }
        }

        private DashboardSnapshot Snapshot()
        {
            var snap = new DashboardSnapshot();
            var nic = FindActiveInterface();

            if (nic != null)
            {
                var props = nic.GetIPProperties();

                snap.InterfaceName = nic.Name;
                snap.InterfaceType = FriendlyKind(nic.NetworkInterfaceType);
                snap.MacAddress = string.Join(":",
                    nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                snap.Status = nic.OperationalStatus == OperationalStatus.Up ? "Up" : nic.OperationalStatus.ToString();

                var ipv4 = props.UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                snap.IpAddress = ipv4?.Address.ToString() ?? "—";

                snap.Gateway = props.GatewayAddresses.FirstOrDefault()?.Address.ToString() ?? "—";
                snap.DnsServer = props.DnsAddresses.FirstOrDefault()?.ToString() ?? "—";

                // Throughput = counter delta over the elapsed interval.
                var stats = nic.GetIPStatistics();
                long rx = stats.BytesReceived, tx = stats.BytesSent;
                var now = DateTime.UtcNow;
                if (_lastRx >= 0)
                {
                    double secs = Math.Max(0.05, (now - _lastTick).TotalSeconds);
                    snap.DownloadMbps = Math.Max(0, rx - _lastRx) * 8 / secs / 1_000_000;
                    snap.UploadMbps = Math.Max(0, tx - _lastTx) * 8 / secs / 1_000_000;
                }
                _lastRx = rx;
                _lastTx = tx;
                _lastTick = now;
            }
            else
            {
                _lastRx = -1; // force a re-baseline when a NIC appears
            }

            return snap;
        }

        /// <summary>Best-guess "the" interface: up, not virtual/loopback,
        /// with an IPv4 address — preferring physical adapters.</summary>
        private static NetworkInterface FindActiveInterface() =>
            NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                            && !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                            && n.GetIPProperties().UnicastAddresses.Any(
                                a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
                .OrderByDescending(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                                        or NetworkInterfaceType.Wireless80211)
                .FirstOrDefault();

        private static string FriendlyKind(NetworkInterfaceType type) => type switch
        {
            NetworkInterfaceType.Wireless80211 => "Wi-Fi",
            NetworkInterfaceType.Ethernet => "Ethernet",
            NetworkInterfaceType.Ppp => "PPP",
            _ => type.ToString()
        };

        /// <summary>Marshals event raises to the UI thread if constructed there.</summary>
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
            _cts.Dispose();
        }
    }
}
