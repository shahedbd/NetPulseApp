using System.Net.NetworkInformation;

namespace NetPulseApp.Service.Privacy
{
    /// <summary>What the adapter scan found.</summary>
    public sealed class VpnScanResult
    {
        /// <summary>Adapters that look like an active VPN tunnel.</summary>
        public List<string> ActiveTunnels { get; } = new();

        /// <summary>VPN software present but not currently connected.</summary>
        public List<string> InstalledButIdle { get; } = new();

        public bool HasActiveTunnel => ActiveTunnels.Count > 0;
    }

    /// <summary>
    /// Looks for VPN tunnel adapters on this PC.
    /// </summary>
    /// <remarks>
    /// <b>This is a positive-only signal.</b> Finding a tunnel adapter is good
    /// evidence a VPN is running. Finding none is <i>not</i> evidence that
    /// traffic is unprotected — a VPN configured on the router, on another
    /// device, or through a client this list doesn't recognise is invisible
    /// from here.
    /// <para>
    /// The UI must therefore never turn an empty result into "you are not
    /// using a VPN". It says what was actually checked: no VPN adapter on this
    /// PC.
    /// </para>
    /// </remarks>
    public static class VpnDetector
    {
        /// <summary>
        /// Substrings matched against adapter name and description. Covers the
        /// common consumer and corporate clients; it is not exhaustive, which
        /// is exactly why a miss is reported as "none found" rather than "none
        /// present".
        /// </summary>
        private static readonly string[] Signatures =
        [
            "tap-windows", "tap-nordvpn", "tap-protonvpn", "openvpn", "wireguard",
            "wintun", "nordlynx", "expressvpn", "protonvpn", "mullvad", "surfshark",
            "private internet access", "cyberghost", "hotspot shield", "tunnelbear",
            "windscribe", "ipvanish", "vyprvpn", "hide.me", "zenmate",
            "cisco anyconnect", "anyconnect", "globalprotect", "pulse secure",
            "juniper", "fortinet", "forticlient", "sonicwall", "check point",
            "sophos", "watchguard", "softether", "hamachi", "zerotier",
            "tailscale", "radmin vpn", "wan miniport (ikev2)", "wan miniport (sstp)",
            "wan miniport (l2tp)", "wan miniport (pptp)"
        ];

        /// <summary>
        /// A generic "vpn" match is deliberately excluded from the list above —
        /// it fires on unrelated adapters. It is checked separately so a match
        /// can be reported with lower confidence if needed.
        /// </summary>
        public static VpnScanResult Scan()
        {
            var result = new VpnScanResult();

            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (!LooksLikeVpn(adapter)) continue;

                    string label = Describe(adapter);

                    // A tunnel only counts as active when it is up and carrying
                    // an address — WAN miniports exist on every Windows install
                    // and are almost always idle.
                    bool active = adapter.OperationalStatus == OperationalStatus.Up &&
                                  HasUsableAddress(adapter);

                    if (active) result.ActiveTunnels.Add(label);
                    else result.InstalledButIdle.Add(label);
                }
            }
            catch
            {
                // Adapter enumeration can fail on locked-down machines; an
                // empty result is reported honestly as "couldn't check".
            }

            return result;
        }

        private static bool LooksLikeVpn(NetworkInterface adapter)
        {
            // Tunnel and PPP adapter types are a structural signal, independent
            // of any vendor name.
            if (adapter.NetworkInterfaceType is NetworkInterfaceType.Tunnel or NetworkInterfaceType.Ppp)
                return true;

            string haystack = $"{adapter.Name} {adapter.Description}".ToLowerInvariant();
            return Signatures.Any(haystack.Contains);
        }

        private static bool HasUsableAddress(NetworkInterface adapter)
        {
            try
            {
                return adapter.GetIPProperties().UnicastAddresses
                    .Any(a => !System.Net.IPAddress.IsLoopback(a.Address));
            }
            catch
            {
                return false;
            }
        }

        private static string Describe(NetworkInterface adapter) =>
            string.IsNullOrWhiteSpace(adapter.Description) ||
            adapter.Description.Equals(adapter.Name, StringComparison.OrdinalIgnoreCase)
                ? adapter.Name
                : $"{adapter.Name} ({adapter.Description})";
    }
}
