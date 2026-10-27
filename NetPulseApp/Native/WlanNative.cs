using System.Runtime.InteropServices;

namespace NetPulseApp.Native
{
    /// <summary>
    /// P/Invoke surface for the Native Wifi API (wlanapi.dll). Variable-length
    /// structs (lists, BSS entries, connection attributes) are read with
    /// Marshal.Read* at the documented byte offsets instead of marshalled
    /// structs — the layouts are fixed by the Windows SDK and this avoids
    /// array-of-struct marshalling pitfalls. Every pointer returned by a
    /// Wlan* call must be released with <see cref="WlanFreeMemory"/>.
    /// </summary>
    internal static class WlanNative
    {
        private const string Dll = "wlanapi.dll";

        // ── Error codes ───────────────────────────────────────────────────────
        public const int ERROR_SUCCESS = 0;
        public const int ERROR_ACCESS_DENIED = 5;
        public const int ERROR_INVALID_PARAMETER = 87;
        public const int ERROR_SERVICE_NOT_ACTIVE = 1062;
        public const int ERROR_NOT_FOUND = 1168;
        public const int ERROR_INVALID_STATE = 5023;

        // ── Notification ──────────────────────────────────────────────────────
        public const int WLAN_NOTIFICATION_SOURCE_NONE = 0;
        public const int WLAN_NOTIFICATION_SOURCE_ACM = 0x00000008;
        public const int ACM_SCAN_COMPLETE = 7;
        public const int ACM_SCAN_FAIL = 8;
        public const int ACM_CONNECTION_COMPLETE = 10;
        public const int ACM_DISCONNECTED = 21;

        // ── Interface opcodes (WLAN_INTF_OPCODE) ──────────────────────────────
        public const int OPCODE_RADIO_STATE = 4;
        public const int OPCODE_CURRENT_CONNECTION = 7;
        public const int OPCODE_CHANNEL_NUMBER = 8;
        public const int OPCODE_RSSI = 0x10000102;

        // ── Enums used as raw ints ────────────────────────────────────────────
        public const int INTERFACE_STATE_CONNECTED = 1;
        public const int DOT11_BSS_TYPE_INFRASTRUCTURE = 1;
        public const int DOT11_BSS_TYPE_ANY = 3;
        public const int DOT11_RADIO_STATE_OFF = 2;

        // ── Struct sizes / offsets (x86 and x64 identical up to the pointer) ──
        public const int LIST_HEADER = 8;                 // dwNumberOfItems + dwIndex / dwTotalSize
        public const int INTERFACE_INFO_SIZE = 532;       // GUID + WCHAR[256] + state
        public const int BSS_ENTRY_SIZE = 360;
        public const int AVAILABLE_NETWORK_SIZE = 628;
        public const int DOT11_SSID_SIZE = 36;            // ULONG len + UCHAR[32]

        public delegate void WlanNotificationCallback(IntPtr data, IntPtr context);

        [DllImport(Dll)]
        public static extern int WlanOpenHandle(
            int dwClientVersion, IntPtr pReserved, out int pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport(Dll)]
        public static extern int WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport(Dll)]
        public static extern int WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport(Dll)]
        public static extern int WlanScan(
            IntPtr hClientHandle, ref Guid pInterfaceGuid, IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

        [DllImport(Dll)]
        public static extern int WlanRegisterNotification(
            IntPtr hClientHandle, int dwNotifSource, bool bIgnoreDuplicate,
            WlanNotificationCallback? funcCallback, IntPtr pCallbackContext,
            IntPtr pReserved, out int pdwPrevNotifSource);

        [DllImport(Dll)]
        public static extern int WlanGetNetworkBssList(
            IntPtr hClientHandle, ref Guid pInterfaceGuid, IntPtr pDot11Ssid,
            int dot11BssType, bool bSecurityEnabled, IntPtr pReserved, out IntPtr ppWlanBssList);

        [DllImport(Dll)]
        public static extern int WlanGetAvailableNetworkList(
            IntPtr hClientHandle, ref Guid pInterfaceGuid, int dwFlags,
            IntPtr pReserved, out IntPtr ppAvailableNetworkList);

        [DllImport(Dll)]
        public static extern int WlanQueryInterface(
            IntPtr hClientHandle, ref Guid pInterfaceGuid, int opCode, IntPtr pReserved,
            out int pdwDataSize, out IntPtr ppData, out int pWlanOpcodeValueType);

        [DllImport(Dll, CharSet = CharSet.Unicode)]
        public static extern int WlanDeleteProfile(
            IntPtr hClientHandle, ref Guid pInterfaceGuid, string strProfileName, IntPtr pReserved);

        [DllImport(Dll)]
        public static extern void WlanFreeMemory(IntPtr pMemory);

        // ══════════════════════════════════════════════════════════════════════
        //  RAW READ HELPERS
        // ══════════════════════════════════════════════════════════════════════

        public static Guid ReadGuid(IntPtr p)
        {
            var bytes = new byte[16];
            Marshal.Copy(p, bytes, 0, 16);
            return new Guid(bytes);
        }

        /// <summary>Reads a DOT11_SSID. SSIDs are raw bytes; UTF-8 is what
        /// Windows and nearly every router use, so emoji / CJK display right.</summary>
        public static string ReadSsid(IntPtr p)
        {
            int len = Math.Clamp(Marshal.ReadInt32(p), 0, 32);
            if (len == 0) return string.Empty;
            var bytes = new byte[len];
            Marshal.Copy(p + 4, bytes, 0, len);
            return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }

        public static string ReadMac(IntPtr p)
        {
            var bytes = new byte[6];
            Marshal.Copy(p, bytes, 0, 6);
            return string.Join(":", bytes.Select(b => b.ToString("X2")));
        }

        /// <summary>Builds an unmanaged DOT11_SSID for SSID-filtered calls.
        /// Caller frees it with Marshal.FreeHGlobal.</summary>
        public static IntPtr AllocSsid(string ssid)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(ssid);
            int len = Math.Min(bytes.Length, 32);
            IntPtr p = Marshal.AllocHGlobal(DOT11_SSID_SIZE);
            var buffer = new byte[DOT11_SSID_SIZE];
            BitConverter.GetBytes(len).CopyTo(buffer, 0);
            Array.Copy(bytes, 0, buffer, 4, len);
            Marshal.Copy(buffer, 0, p, DOT11_SSID_SIZE);
            return p;
        }
    }

    /// <summary>Byte offsets inside the WLAN structs read by WifiAnalyzerService.</summary>
    internal static class WlanOffsets
    {
        // WLAN_INTERFACE_INFO
        public const int IfDescription = 16;
        public const int IfState = 528;

        // WLAN_BSS_ENTRY
        public const int BssSsid = 0;
        public const int BssBssid = 40;
        public const int BssPhyType = 52;
        public const int BssRssi = 56;
        public const int BssLinkQuality = 60;
        public const int BssCenterFreq = 92;
        public const int BssIeOffset = 352;
        public const int BssIeSize = 356;

        // WLAN_AVAILABLE_NETWORK
        public const int NetSsid = 512;
        public const int NetSecurityEnabled = 608;
        public const int NetAuth = 612;
        public const int NetCipher = 616;

        // WLAN_CONNECTION_ATTRIBUTES
        public const int ConnState = 0;
        public const int ConnSsid = 520;
        public const int ConnBssid = 560;
        public const int ConnPhyType = 568;
        public const int ConnSignalQuality = 576;
        public const int ConnRxRate = 580;
        public const int ConnTxRate = 584;
        public const int ConnSecurityEnabled = 588;
        public const int ConnAuth = 596;
        public const int ConnCipher = 600;

        // WLAN_NOTIFICATION_DATA
        public const int NotifSource = 0;
        public const int NotifCode = 4;
        public const int NotifInterface = 8;
    }
}
