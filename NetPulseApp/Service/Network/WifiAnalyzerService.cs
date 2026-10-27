using System.Diagnostics;
using System.Runtime.InteropServices;
using NetPulseApp.Model;
using NetPulseApp.Native;
using static NetPulseApp.Native.WlanNative;

namespace NetPulseApp.Service.Network
{
    /// <summary>
    /// Wi-Fi Analyzer data source — Native Wifi API (wlanapi.dll) only, so it
    /// is language-independent and needs no admin rights or process spawns.
    /// Owns one WLAN client handle for its lifetime; every call runs on the
    /// thread pool. Events are raised on a worker thread — UI callers must
    /// marshal (BeginInvoke) themselves.
    /// </summary>
    public sealed partial class WifiAnalyzerService : IDisposable
    {
        private const int ScanTimeoutMs = 10_000;

        private readonly object _sync = new();
        private IntPtr _handle = IntPtr.Zero;
        private WlanNotificationCallback? _callback;   // field keeps the delegate alive for native code
        private TaskCompletionSource<bool>? _scanTcs;
        private int _scanning;
        private bool _disposed;

        private List<WifiInterfaceInfo> _interfaces = new();
        private Guid _interfaceId;
        private volatile List<WifiAccessPoint> _lastBss = new();

        public WifiAnalyzerState State { get; private set; } = WifiAnalyzerState.Ok;
        public bool IsScanning => Volatile.Read(ref _scanning) == 1;

        /// <summary>True when the last scan reported failure (results were kept).</summary>
        public bool LastScanFailed { get; private set; }

        /// <summary>Adapter chosen in the page's drop-down; null = the connected one.</summary>
        public Guid? PreferredInterface { get; set; }

        public event EventHandler<IReadOnlyList<WifiAccessPoint>>? ScanCompleted;
        public event EventHandler<WifiAnalyzerState>? StateChanged;

        // ══════════════════════════════════════════════════════════════════════
        //  PUBLIC API
        // ══════════════════════════════════════════════════════════════════════

        public IReadOnlyList<WifiInterfaceInfo> GetInterfaces()
        {
            lock (_sync) return _interfaces.ToList();
        }

        /// <summary>Triggers a scan and raises <see cref="ScanCompleted"/> with
        /// the BSS list. Returns false if a scan is already running.</summary>
        public async Task<bool> ScanAsync(CancellationToken ct)
        {
            if (Interlocked.Exchange(ref _scanning, 1) == 1) return false;
            try
            {
                await Task.Run(() => ScanCoreAsync(ct), ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WifiAnalyzerService] Scan: {ex.Message}");
                LastScanFailed = true;
            }
            finally
            {
                Volatile.Write(ref _scanning, 0);
            }
            return true;
        }

        public Task<WifiConnectionInfo?> GetCurrentConnectionAsync(CancellationToken ct)
            => Task.Run(() => ct.IsCancellationRequested ? null : ReadCurrentConnection(), ct);

        // ══════════════════════════════════════════════════════════════════════
        //  SCAN
        // ══════════════════════════════════════════════════════════════════════

        private async Task ScanCoreAsync(CancellationToken ct)
        {
            LastScanFailed = false;

            var ready = EnsureReady();
            if (ready != WifiAnalyzerState.Ok) { SetState(ready); return; }

            Guid id = _interfaceId;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _scanTcs = tcs;

            int rc = WlanScan(_handle, ref id, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if (rc == ERROR_ACCESS_DENIED) { SetState(WifiAnalyzerState.LocationDenied); return; }

            if (rc == ERROR_SUCCESS)
            {
                // Wait for scan_complete / scan_fail; on timeout read whatever is cached (FR-5.3).
                var done = await Task.WhenAny(tcs.Task, Task.Delay(ScanTimeoutMs, ct));
                ct.ThrowIfCancellationRequested();
                if (done == tcs.Task && !tcs.Task.Result)
                {
                    LastScanFailed = true;
                    return;
                }
            }
            else
            {
                Debug.WriteLine($"[WifiAnalyzerService] WlanScan rc={rc}");
                if (IsRadioOff(id)) { SetState(WifiAnalyzerState.RadioOff); return; }
            }

            var list = ReadBssList(id, IntPtr.Zero, false, out rc);
            if (rc == ERROR_ACCESS_DENIED) { SetState(WifiAnalyzerState.LocationDenied); return; }
            if (rc != ERROR_SUCCESS)
            {
                // Adapter unplugged mid-scan, etc. — re-check so the right state shows.
                var again = EnsureReady();
                if (again != WifiAnalyzerState.Ok) SetState(again);
                else LastScanFailed = true;
                return;
            }

            ApplySecurity(id, list);
            _lastBss = list;
            SetState(WifiAnalyzerState.Ok);
            ScanCompleted?.Invoke(this, list);
        }

        // ══════════════════════════════════════════════════════════════════════
        //  HANDLE / INTERFACES / STATE
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Opens the handle if needed, re-enumerates adapters and picks
        /// one. Returns Ok, NoAdapter or RadioOff — never sets State itself.</summary>
        private WifiAnalyzerState EnsureReady()
        {
            lock (_sync)
            {
                if (_disposed) return WifiAnalyzerState.Error;

                if (_handle == IntPtr.Zero)
                {
                    int rc = WlanOpenHandle(2, IntPtr.Zero, out _, out _handle);
                    if (rc != ERROR_SUCCESS) { _handle = IntPtr.Zero; return WifiAnalyzerState.NoAdapter; }

                    _callback = OnNotification;
                    WlanRegisterNotification(_handle, WLAN_NOTIFICATION_SOURCE_ACM, true,
                        _callback, IntPtr.Zero, IntPtr.Zero, out _);
                }

                _interfaces = EnumInterfaces();
                if (_interfaces.Count == 0) return WifiAnalyzerState.NoAdapter;

                var chosen = _interfaces.FirstOrDefault(i => i.Id == PreferredInterface)
                          ?? _interfaces.FirstOrDefault(i => i.IsConnected)
                          ?? _interfaces[0];
                _interfaceId = chosen.Id;
            }

            return IsRadioOff(_interfaceId) ? WifiAnalyzerState.RadioOff : WifiAnalyzerState.Ok;
        }

        private List<WifiInterfaceInfo> EnumInterfaces()
        {
            var result = new List<WifiInterfaceInfo>();
            if (WlanEnumInterfaces(_handle, IntPtr.Zero, out IntPtr list) != ERROR_SUCCESS) return result;

            try
            {
                int count = Marshal.ReadInt32(list);
                for (int i = 0; i < count; i++)
                {
                    IntPtr item = list + LIST_HEADER + i * INTERFACE_INFO_SIZE;
                    result.Add(new WifiInterfaceInfo
                    {
                        Id = ReadGuid(item),
                        Description = Marshal.PtrToStringUni(item + WlanOffsets.IfDescription) ?? "",
                        IsConnected = Marshal.ReadInt32(item + WlanOffsets.IfState) == INTERFACE_STATE_CONNECTED
                    });
                }
            }
            finally { WlanFreeMemory(list); }

            return result;
        }

        /// <summary>WLAN_RADIO_STATE: off when every PHY is software- or hardware-off.</summary>
        private bool IsRadioOff(Guid id)
        {
            if (WlanQueryInterface(_handle, ref id, OPCODE_RADIO_STATE, IntPtr.Zero,
                    out _, out IntPtr data, out _) != ERROR_SUCCESS) return false;
            try
            {
                int phys = Marshal.ReadInt32(data);
                if (phys <= 0) return false;
                for (int i = 0; i < phys; i++)
                {
                    IntPtr phy = data + 4 + i * 12;
                    bool sw = Marshal.ReadInt32(phy + 4) == DOT11_RADIO_STATE_OFF;
                    bool hw = Marshal.ReadInt32(phy + 8) == DOT11_RADIO_STATE_OFF;
                    if (!sw && !hw) return false;
                }
                return true;
            }
            finally { WlanFreeMemory(data); }
        }

        private void SetState(WifiAnalyzerState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(this, state);
        }

        private void OnNotification(IntPtr data, IntPtr context)
        {
            try
            {
                if (Marshal.ReadInt32(data + WlanOffsets.NotifSource) != WLAN_NOTIFICATION_SOURCE_ACM) return;
                int code = Marshal.ReadInt32(data + WlanOffsets.NotifCode);
                if (ReadGuid(data + WlanOffsets.NotifInterface) != _interfaceId) return;

                if (code == ACM_SCAN_COMPLETE) _scanTcs?.TrySetResult(true);
                else if (code == ACM_SCAN_FAIL) _scanTcs?.TrySetResult(false);
            }
            catch { /* never throw back into wlanapi */ }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;
                _scanTcs?.TrySetResult(false);

                if (_handle != IntPtr.Zero)
                {
                    WlanRegisterNotification(_handle, WLAN_NOTIFICATION_SOURCE_NONE, true,
                        null, IntPtr.Zero, IntPtr.Zero, out _);
                    WlanCloseHandle(_handle, IntPtr.Zero);
                    _handle = IntPtr.Zero;
                }
            }
        }
    }
}
