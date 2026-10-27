namespace NetPulseApp.Model;

public enum WifiBand { Unknown, Ghz24, Ghz5, Ghz6 }

/// <summary>One access point (BSS) found by a Wi-Fi scan.</summary>
public class WifiAccessPoint
{
    public string Ssid { get; set; } = string.Empty;
    public string Bssid { get; set; } = string.Empty;
    public int Rssi { get; set; }
    public int Quality { get; set; }
    public int FrequencyMHz { get; set; }
    public WifiBand Band { get; set; }
    public int Channel { get; set; }
    public int WidthMHz { get; set; } = 20;

    /// <summary>2.4 GHz 40 MHz only: +1 secondary above, -1 below, 0 none.</summary>
    public int SecondaryOffset { get; set; }

    public string Security { get; set; } = string.Empty;
    public bool IsOpen { get; set; }
    public bool IsWep { get; set; }
    public string Standard { get; set; } = string.Empty;

    public bool IsSaved { get; set; }
    public bool IsConnected { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.Now;

    /// <summary>Scans in a row this BSS was missing from (0 = in the latest scan).</summary>
    public int MissedScans { get; set; }

    public bool IsHidden => string.IsNullOrEmpty(Ssid);
    public string DisplaySsid => IsHidden ? "(Hidden network)" : Ssid;

    /// <summary>First 5 bytes of the MAC — other BSSIDs of the same radio share it.</summary>
    public string RadioPrefix => Bssid.Length >= 14 ? Bssid[..14] : Bssid;

    public string BandLabel => Band switch
    {
        WifiBand.Ghz24 => "2.4",
        WifiBand.Ghz5 => "5",
        WifiBand.Ghz6 => "6",
        _ => "?"
    };

    public WifiAccessPoint Clone() => (WifiAccessPoint)MemberwiseClone();
}
