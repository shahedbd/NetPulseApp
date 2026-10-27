// Service/Network/SubnetCalculator.cs
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace NetPulseApp.Service.Network
{
    /// <summary>One labelled result row, e.g. ("Network", "192.168.1.0").</summary>
    public record SubnetField(string Name, string Value);

    /// <summary>
    /// Subnet calculator behind the Subnet Calculator page. Pure math, no I/O.
    /// IPv4: CIDR ↔ mask, network, broadcast, host range/count, wildcard.
    /// IPv6: prefix basics (network, range, address count).
    /// Accepts "10.0.0.5/24", "10.0.0.5 255.255.255.0", "10.0.0.5" (= /32)
    /// and "2001:db8::1/64".
    /// </summary>
    public static class SubnetCalculator
    {
        /// <summary>Parses and calculates. Returns false with a user-facing error on bad input.</summary>
        public static bool TryCalculate(string input, out List<SubnetField> fields, out string error)
        {
            fields = new List<SubnetField>();
            error = "";

            if (!TryParse(input, out var ip, out int prefix, out error))
                return false;

            fields = ip.AddressFamily == AddressFamily.InterNetwork
                ? CalculateV4(ip, prefix)
                : CalculateV6(ip, prefix);
            return true;
        }

        private static bool TryParse(string input, out IPAddress ip, out int prefix, out string error)
        {
            ip = null;
            prefix = -1;
            error = "";

            var parts = (input ?? "").Trim()
                .Split(new[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is 0 or > 2 || !IPAddress.TryParse(parts[0], out ip))
            {
                error = "Enter an address like 192.168.1.10/24 or 2001:db8::/64.";
                return false;
            }

            bool v4 = ip.AddressFamily == AddressFamily.InterNetwork;
            int max = v4 ? 32 : 128;

            if (parts.Length == 1)
                prefix = max;
            else if (int.TryParse(parts[1], out int p))
                prefix = p;
            else if (v4 && IPAddress.TryParse(parts[1], out var mask))
                prefix = MaskToPrefix(mask);

            if (prefix < 0 || prefix > max)
            {
                error = v4 ? "Prefix must be /0–/32 or a valid subnet mask." : "Prefix must be /0–/128.";
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // IPv4
        // ─────────────────────────────────────────────────────────────────────

        private static List<SubnetField> CalculateV4(IPAddress ip, int prefix)
        {
            uint addr = ToUInt(ip);
            uint mask = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
            uint network = addr & mask;
            uint broadcast = network | ~mask;

            // /31 (RFC 3021 point-to-point) and /32 have no network/broadcast reservation.
            ulong total = 1UL << (32 - prefix);
            ulong usable = prefix >= 31 ? total : total - 2;
            uint first = prefix >= 31 ? network : network + 1;
            uint last = prefix >= 31 ? broadcast : broadcast - 1;

            return new List<SubnetField>
            {
                new("Address", ip.ToString()),
                new("CIDR notation", $"{FromUInt(network)}/{prefix}"),
                new("Subnet mask", FromUInt(mask)),
                new("Wildcard mask", FromUInt(~mask)),
                new("Network address", FromUInt(network)),
                new("Broadcast address", prefix >= 31 ? "—" : FromUInt(broadcast)),
                new("First usable host", FromUInt(first)),
                new("Last usable host", FromUInt(last)),
                new("Usable hosts", usable.ToString("N0")),
                new("Total addresses", total.ToString("N0")),
                new("Address type", ClassifyV4(addr)),
                new("Binary mask", BinaryMask(mask))
            };
        }

        /// <summary>-1 when the mask's bits aren't contiguous (e.g. 255.0.255.0).</summary>
        private static int MaskToPrefix(IPAddress mask)
        {
            if (mask.AddressFamily != AddressFamily.InterNetwork) return -1;
            uint m = ToUInt(mask);
            int prefix = BitOperations.PopCount(m);
            uint expected = prefix == 0 ? 0u : uint.MaxValue << (32 - prefix);
            return m == expected ? prefix : -1;
        }

        private static string ClassifyV4(uint a)
        {
            byte b1 = (byte)(a >> 24), b2 = (byte)(a >> 16);
            if (b1 == 10 || (b1 == 172 && b2 >= 16 && b2 <= 31) || (b1 == 192 && b2 == 168)) return "Private (RFC 1918)";
            if (b1 == 127) return "Loopback";
            if (b1 == 169 && b2 == 254) return "Link-local (APIPA)";
            if (b1 == 100 && b2 >= 64 && b2 <= 127) return "Carrier-grade NAT (RFC 6598)";
            if (b1 >= 224 && b1 <= 239) return "Multicast";
            if (b1 >= 240) return "Reserved";
            return "Public";
        }

        private static string BinaryMask(uint mask) =>
            string.Join(".", Enumerable.Range(0, 4)
                .Select(i => Convert.ToString((byte)(mask >> (24 - i * 8)), 2).PadLeft(8, '0')));

        private static uint ToUInt(IPAddress ip)
        {
            var b = ip.GetAddressBytes();
            return (uint)(b[0] << 24 | b[1] << 16 | b[2] << 8 | b[3]);
        }

        private static string FromUInt(uint v) =>
            new IPAddress(new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v }).ToString();

        // ─────────────────────────────────────────────────────────────────────
        // IPv6 (prefix basics)
        // ─────────────────────────────────────────────────────────────────────

        private static List<SubnetField> CalculateV6(IPAddress ip, int prefix)
        {
            var bytes = ip.GetAddressBytes();
            var network = new byte[16];
            var last = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                int bits = Math.Clamp(prefix - i * 8, 0, 8);
                byte mask = (byte)(bits == 0 ? 0 : 0xFF << (8 - bits));
                network[i] = (byte)(bytes[i] & mask);
                last[i] = (byte)(network[i] | ~mask);
            }

            var netAddr = new IPAddress(network);
            int hostBits = 128 - prefix;
            string count = hostBits <= 64
                ? (BigInteger.One << hostBits).ToString("N0")
                : $"2^{hostBits}";

            return new List<SubnetField>
            {
                new("Address", ip.ToString()),
                new("CIDR notation", $"{netAddr}/{prefix}"),
                new("Network prefix", netAddr.ToString()),
                new("First address", netAddr.ToString()),
                new("Last address", new IPAddress(last).ToString()),
                new("Total addresses", count),
                new("/64 subnets", prefix <= 64 ? $"{(BigInteger.One << (64 - prefix)):N0}" : "—"),
                new("Address type", ClassifyV6(ip))
            };
        }

        private static string ClassifyV6(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return "Loopback";
            if (ip.IsIPv6LinkLocal) return "Link-local";
            if (ip.IsIPv6Multicast) return "Multicast";
            if (ip.IsIPv6UniqueLocal) return "Unique local (ULA)";
            if (ip.IsIPv4MappedToIPv6) return "IPv4-mapped";
            var b = ip.GetAddressBytes();
            if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8) return "Documentation";
            return (b[0] & 0xE0) == 0x20 ? "Global unicast" : "Reserved / other";
        }
    }
}
