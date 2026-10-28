using NetPulseApp.Forms;
using NetPulseApp.Managers;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Reset — every other category's ResetToDefaults in one go. The page
    /// owns the categories, so it passes the reset in; tool history, speed
    /// test results and the app's bookkeeping fields (launch count, version)
    /// are left alone.
    /// </summary>
    internal sealed class ResetSettings : SettingsCategory
    {
        public override string Title => "Reset";
        public override string Subtitle => "Start over with the original setup";
        public override bool CanReset => false;

        private const string WhatIsReset =
            "Theme, tray, sidebar, IP lookup and speed test options go back to defaults. Tool history and test results are kept.";

        private readonly Action _resetAll;

        public ResetSettings(Action resetAll) => _resetAll = resetAll;

        protected override void BuildContent()
        {
            AddSection("Reset");
            var card = AddCard();

            var button = MakeButton("Reset all…", (s, e) => ResetAll());
            AddRow(card, "Reset all settings", button, WhatIsReset,
                "defaults", "factory", "restore", "original");
            // After AddRow: theming the row ghost-styles its input, which would reset the color.
            button.ForeColor = Theme.ErrorColor;
        }

        private void ResetAll()
        {
            if (!AlertNotificationService.Confirm(
                    WhatIsReset,
                    "Reset all settings?",
                    AlertMessageType.Warning,
                    "Reset",
                    "Cancel"))
                return;

            _resetAll();
            ToastsNotificationManager.Instance.ShowSuccessNotification("Settings reset to defaults.");
        }
    }
}
