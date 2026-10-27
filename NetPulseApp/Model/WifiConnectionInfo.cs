namespace NetPulseApp.Model;

/// <summary>Why the analyzer can or can't show data.</summary>
public enum WifiAnalyzerState { Ok, NoAdapter, RadioOff, LocationDenied, Error }

/// <summary>One Wi-Fi adapter reported by WlanEnumInterfaces.</summary>
public class WifiInterfaceInfo
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public override string ToString() => Description;
}

/// <summary>Snapshot of the current Wi-Fi connection (refreshed every second).</summary>
public class WifiConnectionInfo
{
    public bool IsConnected { get; set; }
    public string Ssid { get; set; } = string.Empty;
    public string Bssid { get; set; } = string.Empty;
    public int Rssi { get; set; }
    public int Quality { get; set; }
    public WifiBand Band { get; set; }
    public int Channel { get; set; }
    public int WidthMHz { get; set; } = 20;
    public int SpanLow { get; set; }
    public int SpanHigh { get; set; }
    public string Standard { get; set; } = string.Empty;
    public string Security { get; set; } = string.Empty;
    public int RxMbps { get; set; }
    public int TxMbps { get; set; }
    public string AdapterName { get; set; } = string.Empty;

    public string BandText => Band switch
    {
        WifiBand.Ghz24 => "2.4 GHz",
        WifiBand.Ghz5 => "5 GHz",
        WifiBand.Ghz6 => "6 GHz",
        _ => "—"
    };
}
