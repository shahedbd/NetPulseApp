using NetPulseApp.Model;

namespace NetPulseApp.Service.Network
{
    /// <summary>Wi-Fi standard and channel width read from a beacon's IEs.</summary>
    public readonly record struct WifiIeInfo(string Standard, int WidthMHz, int SecondaryOffset);

    /// <summary>
    /// Parses the information-element blob of a WLAN_BSS_ENTRY into the
    /// Wi-Fi standard (4/5/6/6E/7) and channel width. Pure — no I/O, safe
    /// to unit test with captured byte arrays.
    /// </summary>
    public static class WifiIeParser
    {
        private const byte IE_HT_CAPS = 45;
        private const byte IE_HT_OPERATION = 61;
        private const byte IE_VHT_CAPS = 191;
        private const byte IE_VHT_OPERATION = 192;
        private const byte IE_EXTENSION = 255;
        private const byte EXT_HE_CAPS = 35;
        private const byte EXT_HE_OPERATION = 36;
        private const byte EXT_EHT_OPERATION = 106;
        private const byte EXT_EHT_CAPS = 108;

        public static WifiIeInfo Parse(ReadOnlySpan<byte> ies, WifiBand band)
        {
            int gen = 0;        // 4 = n, 5 = ac, 6 = ax, 7 = be
            int width = 20;
            int secondary = 0;

            int i = 0;
            while (i + 2 <= ies.Length)
            {
                byte id = ies[i];
                int len = ies[i + 1];
                if (i + 2 + len > ies.Length) break;
                var data = ies.Slice(i + 2, len);

                switch (id)
                {
                    case IE_HT_CAPS:
                        gen = Math.Max(gen, 4);
                        break;

                    case IE_HT_OPERATION when len >= 2:
                        int offset = data[1] & 0x03;
                        if (offset == 1 || offset == 3)
                        {
                            width = Math.Max(width, 40);
                            secondary = offset == 1 ? 1 : -1;
                        }
                        break;

                    case IE_VHT_CAPS:
                        gen = Math.Max(gen, 5);
                        break;

                    case IE_VHT_OPERATION when len >= 3:
                        width = Math.Max(width, VhtWidth(data[0], data[1], data[2]));
                        break;

                    case IE_EXTENSION when len >= 1:
                        ParseExtension(data, ref gen, ref width);
                        break;
                }

                i += 2 + len;
            }

            return new WifiIeInfo(StandardName(gen, band), width, secondary);
        }

        /// <summary>Maps the connected link's DOT11_PHY_TYPE; null if unknown.</summary>
        public static string? StandardFromPhy(int phyType, WifiBand band) => phyType switch
        {
            7 => StandardName(4, band),
            8 => StandardName(5, band),
            10 => StandardName(6, band),
            11 => StandardName(7, band),
            4 or 5 or 6 or 2 => StandardName(0, band),
            _ => null
        };

        public static string StandardName(int gen, WifiBand band) => gen switch
        {
            7 => "Wi-Fi 7 (802.11be)",
            6 => band == WifiBand.Ghz6 ? "Wi-Fi 6E (802.11ax)" : "Wi-Fi 6 (802.11ax)",
            5 => "Wi-Fi 5 (802.11ac)",
            4 => "Wi-Fi 4 (802.11n)",
            _ => "Legacy (802.11a/b/g)"
        };

        /// <summary>"Wi-Fi 6 (802.11ax)" → "Wi-Fi 6" for narrow table columns.</summary>
        public static string ShortStandard(string standard)
        {
            int p = standard.IndexOf(" (", StringComparison.Ordinal);
            return p > 0 ? standard[..p] : standard;
        }

        // ══════════════════════════════════════════════════════════════════════
        //  INTERNALS
        // ══════════════════════════════════════════════════════════════════════

        private static void ParseExtension(ReadOnlySpan<byte> data, ref int gen, ref int width)
        {
            switch (data[0])
            {
                case EXT_EHT_CAPS:
                    gen = Math.Max(gen, 7);
                    break;

                case EXT_HE_CAPS:
                    gen = Math.Max(gen, 6);
                    break;

                case EXT_HE_OPERATION when data.Length >= 7:
                    width = Math.Max(width, HeOperationWidth(data));
                    break;

                // EHT Operation: ext id(1) params(1) basic MCS(4) [control(1) ccfs0 ccfs1]
                case EXT_EHT_OPERATION when data.Length >= 7 && (data[1] & 0x01) != 0:
                    width = Math.Max(width, (data[6] & 0x07) switch
                    {
                        1 => 40, 2 => 80, 3 => 160, 4 => 320, _ => 20
                    });
                    break;
            }
        }

        // HE Operation: ext id(1) params(3) bss color(1) basic MCS(2)
        //   [VHT op info(3) if bit 14] [max co-hosted(1) if bit 15]
        //   [6 GHz op info(5) if bit 17: primary ch, control(width bits 0-1), ccfs0, ccfs1, min rate]
        private static int HeOperationWidth(ReadOnlySpan<byte> data)
        {
            int parms = data[1] | (data[2] << 8) | (data[3] << 16);
            int pos = 7;
            if ((parms & (1 << 14)) != 0) pos += 3;
            if ((parms & (1 << 15)) != 0) pos += 1;
            if ((parms & (1 << 17)) == 0 || pos + 1 >= data.Length) return 20;

            return (data[pos + 1] & 0x03) switch { 1 => 40, 2 => 80, 3 => 160, _ => 20 };
        }

        // VHT Operation: channel width(1), CCFS0(1), CCFS1(1)
        private static int VhtWidth(byte chWidth, byte ccfs0, byte ccfs1)
        {
            switch (chWidth)
            {
                case 1:
                    if (ccfs1 != 0 && Math.Abs(ccfs1 - ccfs0) == 8) return 160;
                    if (ccfs1 != 0 && Math.Abs(ccfs1 - ccfs0) > 16) return 160; // 80+80
                    return 80;
                case 2:
                case 3:
                    return 160;
                default:
                    return 20;
            }
        }
    }
}
