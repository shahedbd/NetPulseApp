// UserControls/Layout/HeaderControl.SystemSummary.cs
using NetPulseApp.Service;
using NetPulseApp.Service.Icons;

namespace NetPulseApp.UserControls.Layout
{
    /// <summary>
    /// HeaderControl — loads the system summary strip's data (PC name,
    /// processor, RAM, OS and uptime) via SystemInfoService and pushes it into
    /// the strip built in HeaderControl.Summary.cs.
    /// </summary>
    public partial class HeaderControl
    {
        /// <summary>Kicks off the summary load; fire-and-forget from the host.</summary>
        public void LoadSystemSummary() => _ = LoadSystemSummaryAsync();

        private async Task LoadSystemSummaryAsync()
        {
            try
            {
                var summary = await Task.Run(() =>
                {
                    var service = new SystemInfoService();
                    var os = service.GetOperatingSystemInfo();
                    string cpuFullName = service.GetProcessorName();

                    return (
                        computerName: os?.CSName ?? "Unknown PC",
                        osCaption: os?.Caption?.Replace("Microsoft ", "") ?? "Windows",
                        cpuName: TruncateCpuName(cpuFullName),
                        cpuFullName,
                        ramAmount: FormatGigabytes(service.GetTotalPhysicalMemory()));
                });

                SetSummary(new[]
                {
                    new SummaryItem(IconChar.Desktop, summary.computerName,
                        Color.FromArgb(20, 184, 166)),
                    new SummaryItem(IconChar.Microchip, summary.cpuName,
                        Color.FromArgb(239, 68, 68), summary.cpuFullName),
                    new SummaryItem(IconChar.Memory, $"{summary.ramAmount} GB RAM",
                        Color.FromArgb(168, 85, 247)),
                    new SummaryItem(IconChar.Globe, summary.osCaption,
                        Color.FromArgb(14, 165, 233)),
                    new SummaryItem(IconChar.Clock, FormatUptime(),
                        Color.FromArgb(245, 158, 11))
                });
            }
            catch
            {
                SetSummaryText("System information unavailable");
            }
        }

        /// <summary>
        /// Caps the processor name so it can't push the later segments off the
        /// end of the bar. The tooltip still carries the full name.
        /// </summary>
        private static string TruncateCpuName(string name, int maxLength = 26)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length <= maxLength)
                return name;

            int cut = name.LastIndexOf(' ', maxLength - 1);
            string trimmed = (cut > 10 ? name[..cut] : name[..maxLength]).TrimEnd();
            return trimmed + "…";
        }

        private static string FormatGigabytes(ulong bytes)
        {
            const double GB = 1024d * 1024 * 1024;
            return (bytes / GB).ToString("F1");
        }

        private static string FormatUptime()
        {
            var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return uptime.TotalDays < 1
                ? $"Up {(int)uptime.TotalHours}h {uptime.Minutes}m"
                : $"Up {(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        }
    }
}
