// Service/Network/DiagnoseService.cs
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using NetPulseApp.Service.Privacy;

namespace NetPulseApp.Service.Network
{
    public enum CheckStatus { Pass, Warn, Fail, Skipped }

    /// <summary>One diagnose step's outcome — one table row on the page.</summary>
    public class CheckResult
    {
        public string Name { get; set; } = "";
        public CheckStatus Status { get; set; }
        public string Detail { get; set; } = "";
        public long? LatencyMs { get; set; }
    }

    /// <summary>Raw facts gathered by a run — the input to DiagnoseVerdicts.</summary>
    public class DiagnoseReport
    {
        public List<CheckResult> Checks { get; } = new();
        public string AdapterName { get; set; } = "";
        public bool HasAdapter { get; set; }
        public bool HasValidIp { get; set; }
        public bool IsApipa { get; set; }
        public string Gateway { get; set; } = "";
        public bool GatewayReachable { get; set; }
        public bool SystemDnsOk { get; set; }
        public bool PublicDnsOk { get; set; }
        public bool IcmpOk { get; set; }
        public bool TcpOk { get; set; }
        public long? InternetLatencyMs { get; set; }
        public bool HttpsOk { get; set; }
        public bool? CaptivePortal { get; set; }
        public bool VpnActive { get; set; }

        /// <summary>Internet reachable by ICMP or by the TCP 443 fallback.</summary>
        public bool InternetReachable => IcmpOk || TcpOk;
    }

    /// <summary>
    /// "Diagnose My Connection" engine: adapter → valid IP → gateway ping →
    /// DNS (system vs 1.1.1.1) → internet ping (TCP 443 fallback when ICMP
    /// is blocked) → HTTPS → captive portal. Raises one CheckCompleted per
    /// step, then RunFinished with the full report — both marshalled to the
    /// UI thread via the SynchronizationContext captured at Start.
    /// </summary>
    public class DiagnoseService : IDisposable
    {
        private const string InternetHost = "1.1.1.1";
        private const string PublicDns = "1.1.1.1";
        private const string DnsTestName = "www.msftconnecttest.com";
        private const string HttpsTestUrl = "https://www.cloudflare.com/cdn-cgi/trace";
        private const int SlowLatencyMs = 150;

        private CancellationTokenSource _cts;
        private SynchronizationContext _ui;

        public event Action<CheckResult> CheckCompleted;
        public event Action<DiagnoseReport> RunFinished;

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            _ui = SynchronizationContext.Current;
            IsRunning = true;
            _ = Task.Run(() => RunAsync(_cts.Token));
        }

        public void Stop() => _cts?.Cancel();

        private async Task RunAsync(CancellationToken token)
        {
            var report = new DiagnoseReport();
            try
            {
                await RunStepsAsync(report, token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Add(report, "Diagnose", CheckStatus.Fail, $"Unexpected error: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                if (!token.IsCancellationRequested)
                    Post(() => RunFinished?.Invoke(report));
            }
        }

        private async Task RunStepsAsync(DiagnoseReport r, CancellationToken token)
        {
            // 1. Adapter
            var nic = DashboardService.FindActiveInterface();
            r.HasAdapter = nic != null;
            r.VpnActive = VpnDetector.Scan().HasActiveTunnel;
            if (nic == null)
            {
                Add(r, "Network adapter", CheckStatus.Fail, "No connected adapter with an IPv4 address");
                SkipRest(r, "Local IP", "Gateway", "DNS", "Internet", "HTTPS", "Captive portal");
                return;
            }
            r.AdapterName = nic.Name;
            Add(r, "Network adapter", CheckStatus.Pass, $"{nic.Name} ({nic.NetworkInterfaceType})");

            // 2. Valid IP — 169.254.x.x means DHCP never answered.
            var props = nic.GetIPProperties();
            var v4 = props.UnicastAddresses
                .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)?.Address;
            r.IsApipa = v4 != null && v4.GetAddressBytes() is [169, 254, ..];
            r.HasValidIp = v4 != null && !r.IsApipa;
            Add(r, "Local IP", r.HasValidIp ? CheckStatus.Pass : CheckStatus.Fail,
                r.IsApipa ? $"{v4} — self-assigned, DHCP did not respond" : v4?.ToString() ?? "No IPv4 address");

            // 3. Gateway — some routers drop ICMP, so a miss is a warning only.
            r.Gateway = props.GatewayAddresses
                .Select(g => g.Address)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !a.Equals(IPAddress.Any))
                ?.ToString() ?? "";
            if (string.IsNullOrEmpty(r.Gateway))
                Add(r, "Gateway", CheckStatus.Fail, "No default gateway configured");
            else
            {
                var gw = await DiagnoseProbes.PingAsync(r.Gateway, 3, token);
                r.GatewayReachable = gw.HasValue;
                Add(r, "Gateway", gw.HasValue ? CheckStatus.Pass : CheckStatus.Warn,
                    gw.HasValue ? $"{r.Gateway} replied" : $"{r.Gateway} did not reply to ping", gw);
            }

            // 4. DNS — system resolver vs 1.1.1.1 tells "DNS broken" from "internet broken".
            var sys = await DiagnoseProbes.ResolveSystemAsync(DnsTestName, token);
            var pub = await DiagnoseProbes.ResolveWithAsync(PublicDns, DnsTestName, token);
            r.SystemDnsOk = sys.Ok;
            r.PublicDnsOk = pub.Ok;
            Add(r, "DNS", sys.Ok ? CheckStatus.Pass : pub.Ok ? CheckStatus.Fail : CheckStatus.Warn,
                $"System DNS {(sys.Ok ? $"OK ({sys.Ms} ms)" : $"failed ({sys.Error})")} · " +
                $"1.1.1.1 {(pub.Ok ? $"OK ({pub.Ms} ms)" : "failed")}", sys.Ok ? sys.Ms : null);

            // 5. Internet — ICMP first, TCP 443 connect when ICMP is blocked.
            var icmp = await DiagnoseProbes.PingAsync(InternetHost, 3, token);
            r.IcmpOk = icmp.HasValue;
            long? latency = icmp;
            string method = "ping";
            if (!icmp.HasValue)
            {
                latency = await DiagnoseProbes.TcpConnectAsync(InternetHost, 443, token);
                r.TcpOk = latency.HasValue;
                method = "TCP 443 (ping blocked)";
            }
            r.InternetLatencyMs = latency;
            Add(r, "Internet", !r.InternetReachable ? CheckStatus.Fail
                    : latency > SlowLatencyMs ? CheckStatus.Warn : CheckStatus.Pass,
                r.InternetReachable ? $"{InternetHost} via {method}: {latency} ms" : $"{InternetHost} unreachable", latency);

            // 6. HTTPS
            var https = await DiagnoseProbes.HttpsAsync(HttpsTestUrl, token);
            r.HttpsOk = https.Ok;
            Add(r, "HTTPS", https.Ok ? CheckStatus.Pass : CheckStatus.Fail,
                $"cloudflare.com: {https.Detail}", https.Ok ? https.Ms : null);

            // 7. Captive portal
            r.CaptivePortal = await DiagnoseProbes.IsCaptivePortalAsync(token);
            Add(r, "Captive portal", r.CaptivePortal switch
            {
                true => CheckStatus.Fail,
                false => CheckStatus.Pass,
                _ => CheckStatus.Warn
            }, r.CaptivePortal switch
            {
                true => "Traffic is redirected to a sign-in page",
                false => "None detected",
                _ => "Could not check"
            });
        }

        private void SkipRest(DiagnoseReport r, params string[] names)
        {
            foreach (var name in names)
                Add(r, name, CheckStatus.Skipped, "Skipped — no network adapter");
        }

        private void Add(DiagnoseReport r, string name, CheckStatus status, string detail, long? ms = null)
        {
            var check = new CheckResult { Name = name, Status = status, Detail = detail, LatencyMs = ms };
            r.Checks.Add(check);
            Post(() => CheckCompleted?.Invoke(check));
        }

        /// <summary>Marshals event raises to the UI thread if Start was called there.</summary>
        private void Post(Action raise)
        {
            if (_ui != null) _ui.Post(_ => raise(), null);
            else raise();
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
