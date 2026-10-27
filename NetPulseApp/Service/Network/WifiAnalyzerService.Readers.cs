using System.Runtime.InteropServices;
using NetPulseApp.Model;
using NetPulseApp.Native;
using static NetPulseApp.Native.WlanNative;

namespace NetPulseApp.Service.Network
{
    /// <summary>Struct readers: BSS list, available-network security and the
    /// current connection. Every returned WLAN pointer is freed here.</summary>
    public sealed partial class WifiAnalyzerService
    {
        private WifiAccessPoint? _connBss;
        private DateTime _connBssAt;

        // ══════════════════════════════════════════════════════════════════════
        //  BSS LIST
        // ══════════════════════════════════════════════════════════════════════

        private List<WifiAccessPoint> ReadBssList(Guid id, IntPtr ssid, bool securityEnabled, out int rc)
        {
            var result = new List<WifiAccessPoint>();
            int bssType = ssid == IntPtr.Zero ? DOT11_BSS_TYPE_ANY : DOT11_BSS_TYPE_INFRASTRUCTURE;

            rc = WlanGetNetworkBssList(_handle, ref id, ssid, bssType, securityEnabled, IntPtr.Zero, out IntPtr list);
            if (rc != ERROR_SUCCESS) return result;

            try
            {
                int count = Marshal.ReadInt32(list + 4);
                var now = DateTime.Now;
                for (int i = 0; i < count; i++)
                {
                    IntPtr e = list + LIST_HEADER + i * BSS_ENTRY_SIZE;
                    int freq = Marshal.ReadInt32(e + WlanOffsets.BssCenterFreq) / 1000;
                    var (band, channel) = ChannelAnalyzer.FromFrequency(freq);

                    int ieOffset = Marshal.ReadInt32(e + WlanOffsets.BssIeOffset);
                    int ieSize = Marshal.ReadInt32(e + WlanOffsets.BssIeSize);
                    var ies = new byte[Math.Max(0, ieSize)];
                    if (ieSize > 0) Marshal.Copy(e + ieOffset, ies, 0, ieSize);
                    var ie = WifiIeParser.Parse(ies, band);

                    result.Add(new WifiAccessPoint
                    {
                        Ssid = ReadSsid(e + WlanOffsets.BssSsid),
                        Bssid = ReadMac(e + WlanOffsets.BssBssid),
                        Rssi = Marshal.ReadInt32(e + WlanOffsets.BssRssi),
                        Quality = Marshal.ReadInt32(e + WlanOffsets.BssLinkQuality),
                        FrequencyMHz = freq,
                        Band = band,
                        Channel = channel,
                        WidthMHz = ie.WidthMHz,
                        SecondaryOffset = ie.SecondaryOffset,
                        Standard = ie.Standard,
                        LastSeen = now
                    });
                }
            }
            finally { WlanFreeMemory(list); }

            return result;
        }

        /// <summary>Security per SSID comes from the available-network list, not the BSS entry.</summary>
        private void ApplySecurity(Guid id, List<WifiAccessPoint> aps)
        {
            var bySsid = new Dictionary<string, (int Auth, int Cipher, bool Enabled)>();

            if (WlanGetAvailableNetworkList(_handle, ref id, 0, IntPtr.Zero, out IntPtr list) == ERROR_SUCCESS)
            {
                try
                {
                    int count = Marshal.ReadInt32(list);
                    for (int i = 0; i < count; i++)
                    {
                        IntPtr n = list + LIST_HEADER + i * AVAILABLE_NETWORK_SIZE;
                        string ssid = ReadSsid(n + WlanOffsets.NetSsid);
                        if (bySsid.ContainsKey(ssid)) continue;
                        bySsid[ssid] = (Marshal.ReadInt32(n + WlanOffsets.NetAuth),
                                        Marshal.ReadInt32(n + WlanOffsets.NetCipher),
                                        Marshal.ReadInt32(n + WlanOffsets.NetSecurityEnabled) != 0);
                    }
                }
                finally { WlanFreeMemory(list); }
            }

            foreach (var ap in aps)
            {
                if (!bySsid.TryGetValue(ap.Ssid, out var s)) { ap.Security = "—"; continue; }
                ap.Security = WifiSecurityMapper.Label(s.Auth, s.Cipher, s.Enabled);
                ap.IsOpen = WifiSecurityMapper.IsOpen(s.Auth, s.Cipher, s.Enabled);
                ap.IsWep = WifiSecurityMapper.IsWep(s.Auth, s.Cipher);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CURRENT CONNECTION (no scan — FR-1.2)
        // ══════════════════════════════════════════════════════════════════════

        private WifiConnectionInfo? ReadCurrentConnection()
        {
            var ready = EnsureReady();
            if (ready != WifiAnalyzerState.Ok) { SetState(ready); return null; }

            // Recovered from "no adapter" / "radio off": report Ok so the page rescans.
            if (State is WifiAnalyzerState.NoAdapter or WifiAnalyzerState.RadioOff or WifiAnalyzerState.Error)
                SetState(WifiAnalyzerState.Ok);

            Guid id = _interfaceId;
            string adapter = _interfaces.FirstOrDefault(i => i.Id == id)?.Description ?? "";

            int rc = WlanQueryInterface(_handle, ref id, OPCODE_CURRENT_CONNECTION, IntPtr.Zero,
                out _, out IntPtr data, out _);
            if (rc == ERROR_ACCESS_DENIED) { SetState(WifiAnalyzerState.LocationDenied); return null; }
            if (rc != ERROR_SUCCESS) return new WifiConnectionInfo { AdapterName = adapter };

            var info = new WifiConnectionInfo { AdapterName = adapter };
            int phy;
            bool secEnabled;
            try
            {
                if (Marshal.ReadInt32(data + WlanOffsets.ConnState) != INTERFACE_STATE_CONNECTED) return info;

                info.IsConnected = true;
                info.Ssid = ReadSsid(data + WlanOffsets.ConnSsid);
                info.Bssid = ReadMac(data + WlanOffsets.ConnBssid);
                info.Quality = Marshal.ReadInt32(data + WlanOffsets.ConnSignalQuality);
                info.RxMbps = Marshal.ReadInt32(data + WlanOffsets.ConnRxRate) / 1000;
                info.TxMbps = Marshal.ReadInt32(data + WlanOffsets.ConnTxRate) / 1000;
                phy = Marshal.ReadInt32(data + WlanOffsets.ConnPhyType);
                secEnabled = Marshal.ReadInt32(data + WlanOffsets.ConnSecurityEnabled) != 0;
                info.Security = WifiSecurityMapper.Label(
                    Marshal.ReadInt32(data + WlanOffsets.ConnAuth),
                    Marshal.ReadInt32(data + WlanOffsets.ConnCipher), secEnabled);
            }
            finally { WlanFreeMemory(data); }

            var bss = FindConnectedBss(id, info, secEnabled);
            int? rssi = QueryInt(id, OPCODE_RSSI);
            int? channel = QueryInt(id, OPCODE_CHANNEL_NUMBER);

            info.Rssi = rssi is < 0 ? rssi.Value : bss?.Rssi ?? info.Quality / 2 - 100;
            info.Channel = channel is > 0 ? channel.Value : bss?.Channel ?? 0;
            info.Band = bss?.Band ?? (info.Channel > 14 ? WifiBand.Ghz5 : WifiBand.Ghz24);
            info.WidthMHz = bss?.WidthMHz ?? 20;
            info.Standard = WifiIeParser.StandardFromPhy(phy, info.Band) ?? bss?.Standard ?? "—";

            var span = ChannelAnalyzer.Span(info.Band, info.Channel, info.WidthMHz, bss?.SecondaryOffset ?? 0);
            info.SpanLow = span.Low;
            info.SpanHigh = span.High;
            return info;
        }

        /// <summary>Connected BSS from the last scan, else a cached SSID-filtered
        /// BSS read (no new scan). Re-read at most every 10 s or on roaming.</summary>
        private WifiAccessPoint? FindConnectedBss(Guid id, WifiConnectionInfo info, bool secEnabled)
        {
            var hit = _lastBss.FirstOrDefault(a => a.Bssid == info.Bssid);
            if (hit != null) return hit;

            if (_connBss?.Bssid == info.Bssid && DateTime.Now - _connBssAt < TimeSpan.FromSeconds(10))
                return _connBss;

            IntPtr ssid = AllocSsid(info.Ssid);
            try
            {
                var list = ReadBssList(id, ssid, secEnabled, out _);
                _connBss = list.FirstOrDefault(a => a.Bssid == info.Bssid);
                _connBssAt = DateTime.Now;
                return _connBss;
            }
            finally { Marshal.FreeHGlobal(ssid); }
        }

        private int? QueryInt(Guid id, int opcode)
        {
            if (WlanQueryInterface(_handle, ref id, opcode, IntPtr.Zero, out int size, out IntPtr data, out _) != ERROR_SUCCESS)
                return null;
            try { return size >= 4 ? Marshal.ReadInt32(data) : null; }
            finally { WlanFreeMemory(data); }
        }
    }
}
