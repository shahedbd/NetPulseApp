// UserControls/Pages/WifiControl.Run.cs
using System.Diagnostics;
using System.Text;
using NetPulseApp.Helper;
using NetPulseApp.Model;
using NetPulseApp.Service.Icons;
using NetPulseApp.UiComponent;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Wi-Fi page run logic: a 1-second connection poll (skipped while the
    /// page is hidden or the window minimized), card/detail/chart updates,
    /// and the Location-denied / radio-off / no-adapter / not-connected
    /// state panel. Layout/theme live in WifiControl.cs.
    /// </summary>
    public partial class WifiControl
    {
        private readonly System.Windows.Forms.Timer _tick = new() { Interval = 1000 };
        private readonly CancellationTokenSource _cts = new();
        private WifiConnectionInfo? _info;
        private bool _busy;
        private bool _panelShown;
        private WifiAnalyzerState? _shownState; // reason on the panel; null = "not connected"

        private void WirePolling()
        {
            _tick.Tick += async (s, e) => await RefreshAsync();
            _service.StateChanged += (s, state) =>
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(new Action(() => ApplyState(state, _info)));
            };
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(BuildDetails());

            HandleCreated += async (s, e) =>
            {
                RequestLocationOnce();
                _tick.Start();
                await RefreshAsync();
            };
        }

        private static bool _locationRequested;

        /// <summary>Makes Windows show its consent prompt and list the app under
        /// Location settings (SSID/BSSID are location data on Windows 11 24H2+).
        /// Once per app session, as in WiFiPasswordViewer.</summary>
        private static async void RequestLocationOnce()
        {
            if (_locationRequested) return;
            _locationRequested = true;
            try { await global::Windows.Devices.Geolocation.Geolocator.RequestAccessAsync(); }
            catch (Exception ex) { Debug.WriteLine($"[WifiControl] Location request: {ex.Message}"); }
        }

        private void StopPolling()
        {
            _tick.Stop();
            _tick.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        /// <summary>Never poll the Native WiFi API while hidden/minimized — keeps restore snappy.</summary>
        private bool CanPoll()
        {
            var form = FindForm();
            return !IsDisposed && Visible && form is { Visible: true } && form.WindowState != FormWindowState.Minimized;
        }

        private async Task RefreshAsync()
        {
            if (_busy || !CanPoll()) return;
            _busy = true;
            try
            {
                var info = await _service.GetCurrentConnectionAsync(_cts.Token);
                if (IsDisposed) return;
                _info = info;
                ApplyState(_service.State, info);
                if (_service.State == WifiAnalyzerState.Ok && info is { IsConnected: true })
                    ApplyConnection(info);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Debug.WriteLine($"[WifiControl] Connection: {ex.Message}"); }
            finally { _busy = false; }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DISPLAY
        // ─────────────────────────────────────────────────────────────────────

        private void ApplyConnection(WifiConnectionInfo i)
        {
            var level = WifiAnalyzerUIHelper.LevelFromRssi(i.Rssi);
            _cardSignal.SetData($"{i.Rssi} dBm", $"{i.Quality}% quality", WifiAnalyzerUIHelper.LevelColor(level));
            _cardSignal.Badge = level.ToString();
            _cardSignal.BadgeColor = WifiAnalyzerUIHelper.LevelColor(level);
            _cardBand.SetData(i.BandText, i.Channel > 0 ? $"channel {i.Channel}" : "channel —");
            _cardSpeed.SetData($"{i.RxMbps} Mbps", $"↓ {i.RxMbps} / ↑ {i.TxMbps} Mbps link rate");
            _cardSecurity.SetData(Fallback(i.Security), "encryption");

            _detailValues["NETWORK (SSID)"].Text = Fallback(i.Ssid);
            _detailValues["BSSID"].Text = Fallback(i.Bssid);
            _detailValues["STANDARD"].Text = Fallback(i.Standard);
            _detailValues["CHANNEL WIDTH"].Text = $"{i.WidthMHz} MHz";
            _detailValues["ADAPTER"].Text = Fallback(i.AdapterName);
            _lblTip.Text = "ⓘ  " + WifiAnalyzerUIHelper.LevelTip(level);

            _chart.AddSample(i.Rssi, i.Bssid);
        }

        /// <summary>Overlays the state panel when there is nothing live to show.</summary>
        private void ApplyState(WifiAnalyzerState state, WifiConnectionInfo? info)
        {
            bool connected = state == WifiAnalyzerState.Ok && info is { IsConnected: true };
            _statePanel.Visible = !connected;
            _body.Visible = connected;
            if (connected) { _panelShown = false; return; }

            // Polled every second — only rebuild the panel when the reason changes.
            var shown = state == WifiAnalyzerState.Ok ? (WifiAnalyzerState?)null : state;
            if (_panelShown && _shownState == shown) return;
            _panelShown = true;
            _shownState = shown;

            _chart.Reset();
            switch (state)
            {
                case WifiAnalyzerState.LocationDenied:
                    _statePanel.ShowState(IconChar.LocationDot,
                        "Windows needs Location permission to read Wi-Fi details",
                        "Windows treats Wi-Fi network names as location data, so it asks for permission first. " +
                        $"{AppConfig.AppName} never reads, stores or sends your location.\n\n" +
                        "1. Click \"Open Location settings\"\n" +
                        "2. Turn on \"Location services\" and \"Let desktop apps access your location\"\n" +
                        "3. Come back here — the page refreshes on its own",
                        ("Open Location settings", () => OpenSettings("ms-settings:privacy-location")));
                    break;

                case WifiAnalyzerState.RadioOff:
                    _statePanel.ShowState(IconChar.PowerOff, "Wi-Fi is turned off",
                        "Turn on Wi-Fi to see your connection details.",
                        ("Open Wi-Fi settings", () => OpenSettings("ms-settings:network-wifi")));
                    break;

                case WifiAnalyzerState.NoAdapter:
                    _statePanel.ShowState(IconChar.Microchip, "No Wi-Fi adapter found",
                        "This PC has no Wi-Fi adapter, or it is disabled. On Ethernet, see the Dashboard for connection details.",
                        ("Open Network settings", () => OpenSettings("ms-settings:network-status")));
                    break;

                case WifiAnalyzerState.Error:
                    _statePanel.ShowState(IconChar.TriangleExclamation, "Wi-Fi data is unavailable",
                        "Windows did not return Wi-Fi information.",
                        ("Try again", () => _ = RefreshAsync()));
                    break;

                default:
                    _statePanel.ShowState(IconChar.Wifi, "Not connected to Wi-Fi",
                        "Your Wi-Fi adapter is on but not connected to a network.",
                        ("Open Wi-Fi settings", () => OpenSettings("ms-settings:network-wifi")));
                    break;
            }
        }

        private static void OpenSettings(string uri)
        {
            try { Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true }); }
            catch { /* Settings app unavailable */ }
        }

        private static string Fallback(string value) =>
            string.IsNullOrWhiteSpace(value) ? "—" : value;

        private string BuildDetails()
        {
            if (_info is not { IsConnected: true } i) return "";

            var sb = new StringBuilder();
            sb.AppendLine($"Network (SSID):   {i.Ssid}");
            sb.AppendLine($"BSSID:            {i.Bssid}");
            sb.AppendLine($"Signal:           {i.Rssi} dBm, {i.Quality}% ({WifiAnalyzerUIHelper.LevelLabel(i.Rssi)})");
            sb.AppendLine($"Band / channel:   {i.BandText} / {i.Channel} ({i.WidthMHz} MHz)");
            sb.AppendLine($"Standard:         {i.Standard}");
            sb.AppendLine($"Security:         {i.Security}");
            sb.AppendLine($"Link rate:        {i.RxMbps} / {i.TxMbps} Mbps");
            sb.AppendLine($"Adapter:          {i.AdapterName}");
            return sb.ToString();
        }
    }
}
