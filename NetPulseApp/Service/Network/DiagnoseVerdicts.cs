// Service/Network/DiagnoseVerdicts.cs
namespace NetPulseApp.Service.Network
{
    public enum VerdictSeverity { Good, Warning, Problem }

    /// <summary>Plain-English diagnosis plus what to try, shown in the verdict card.</summary>
    public class Verdict
    {
        public VerdictSeverity Severity { get; init; }
        public string Title { get; init; } = "";
        public string Explanation { get; init; } = "";
        public string[] FixSteps { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Ordered rules that turn a DiagnoseReport into one verdict. The first
    /// rule that matches wins, so rules run from the lowest layer (adapter)
    /// up to the highest (HTTPS / captive portal). Steps only — no
    /// settings are changed for the user (decision D7).
    /// </summary>
    public static class DiagnoseVerdicts
    {
        private const int SlowLatencyMs = 150;

        private static readonly (Func<DiagnoseReport, bool> When, Func<DiagnoseReport, Verdict> Then)[] Rules =
        {
            (r => !r.HasAdapter, r => Problem("You're not connected to a network",
                "No network adapter is connected with an IP address.",
                "Turn Wi-Fi on and pick your network, or check that the Ethernet cable is plugged in.",
                "Turn Airplane mode off.",
                "Restart the PC if the adapter is missing entirely.")),

            (r => r.IsApipa, r => Problem("Your router didn't give this PC an address",
                $"{r.AdapterName} has a self-assigned 169.254.x.x address — the router's DHCP did not answer.",
                "Restart your router and modem (unplug for 30 seconds).",
                "Disconnect and reconnect to the network.",
                "Run 'ipconfig /renew' in a Command Prompt.")),

            (r => !r.HasValidIp, r => Problem("No valid IP address",
                "The adapter is connected but has no usable IPv4 address.",
                "Disconnect and reconnect to the network.",
                "Restart your router.")),

            (r => string.IsNullOrEmpty(r.Gateway), r => Problem("No route to your router",
                "The adapter has an address but no default gateway, so traffic can't leave this PC.",
                "Disconnect and reconnect to the network.",
                "If you set a static IP, add the router's address as the gateway.")),

            (r => r.CaptivePortal == true, r => Problem("Sign-in page required",
                "This network (hotel, airport, café) redirects traffic to a login page.",
                "Open a browser and go to any http:// site, e.g. http://neverssl.com, to bring up the sign-in page.",
                "Accept the terms or sign in, then run Diagnose again.")),

            (r => !r.GatewayReachable && !r.InternetReachable, r => Problem("Your router isn't responding",
                $"Neither your router ({r.Gateway}) nor the internet replied.",
                "Restart your router and modem.",
                "Move closer to the router, or try a cable.",
                "Check whether other devices on the network are online.")),

            (r => !r.InternetReachable, r => Problem("Your router is fine, but the internet is down",
                "Your router replies, but nothing beyond it does. This is usually your internet provider or modem.",
                "Restart your modem / ONT, then the router.",
                "Check your provider's outage page or status line.",
                r.VpnActive ? "Disconnect your VPN and try again." : "Check the modem's status lights.")),

            (r => !r.SystemDnsOk && r.PublicDnsOk, r => Problem("DNS isn't working",
                "The internet works, but your configured DNS server can't look up names. Websites won't load by name.",
                "Restart your router (it often relays DNS).",
                "Set DNS to 1.1.1.1 and 1.0.0.1 in Settings › Network & internet › your adapter › DNS server assignment.",
                "Run 'ipconfig /flushdns' in a Command Prompt.")),

            (r => !r.SystemDnsOk && !r.PublicDnsOk, r => Warning("Name lookups are blocked",
                "The internet is reachable, but no DNS server answered — a firewall or network policy may block DNS.",
                "If you're on a work or school network, ask its administrator.",
                "Temporarily disable third-party firewall or security software and retry.")),

            (r => !r.HttpsOk, r => Warning("Secure websites are failing",
                "The internet is reachable, but an HTTPS request failed. A proxy, firewall or wrong system clock can cause this.",
                "Check that Windows date and time are correct.",
                "Turn off any proxy in Settings › Network & internet › Proxy.",
                r.VpnActive ? "Disconnect your VPN and try again." : "Temporarily disable web filtering software.")),

            (r => r.InternetLatencyMs > SlowLatencyMs, r => Warning("Connected, but slow to respond",
                $"Everything works, but latency to the internet is {r.InternetLatencyMs} ms — calls and games may lag.",
                "Pause large downloads or streams on this network.",
                "Move closer to the router or use a cable.",
                r.VpnActive ? "A VPN adds latency — try a closer server." : "Restart the router if it has been on for a long time.")),
        };

        /// <summary>The first matching rule's verdict, or "all good".</summary>
        public static Verdict Evaluate(DiagnoseReport report)
        {
            foreach (var (when, then) in Rules)
                if (when(report)) return then(report);

            return new Verdict
            {
                Severity = VerdictSeverity.Good,
                Title = "Your connection looks healthy",
                Explanation = $"All checks passed — internet latency {report.InternetLatencyMs} ms."
                    + (report.IcmpOk ? "" : " (Ping is blocked on this network; TCP was used instead.)")
            };
        }

        private static Verdict Problem(string title, string why, params string[] steps) =>
            new() { Severity = VerdictSeverity.Problem, Title = title, Explanation = why, FixSteps = steps };

        private static Verdict Warning(string title, string why, params string[] steps) =>
            new() { Severity = VerdictSeverity.Warning, Title = title, Explanation = why, FixSteps = steps };
    }
}
