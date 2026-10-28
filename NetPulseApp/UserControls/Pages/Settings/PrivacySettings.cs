using System.Diagnostics;
using DeviceDataModule;
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Privacy &amp; Data — where settings and history live on disk, and which
    /// outside services the tools contact. Nothing here is a setting, so the
    /// footer's Reset page is hidden.
    /// </summary>
    internal sealed class PrivacySettings : SettingsCategory
    {
        public override string Title => "Privacy & Data";
        public override string Subtitle => "What NetPulse stores and contacts";
        public override bool CanReset => false;

        private static string DataFolder => ApplicationDataManager.Instance.ApplicationDataDirectory;

        protected override void BuildContent()
        {
            AddSection("Stored on this PC");
            var stored = AddCard();
            AddRow(stored, "Settings and history folder", MakeButton("Open folder", (s, e) => OpenDataFolder()),
                DataFolder,
                "folder", "appdata", "files", "location", "storage", "data");

            AddSection("Contacted from this PC");
            var sent = AddCard();
            var note = sent.AddFull(MakeNote(
                "Network tools contact the hosts you enter, plus ipwho.is, ipapi.co and Cloudflare " +
                "for your public IP and speed tests. Results stay on this PC."));
            Register(note, "Services contacted", "Hosts and services the tools connect to",
                "privacy", "internet", "cloudflare", "ipwho", "ipapi", "network");
        }

        private static void OpenDataFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{DataFolder}\"") { UseShellExecute = true });
            }
            catch
            {
                ToastsNotificationManager.Instance.ShowErrorNotification("Couldn't open the data folder.");
            }
        }
    }
}
