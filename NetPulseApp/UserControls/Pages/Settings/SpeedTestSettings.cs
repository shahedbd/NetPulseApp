using DeviceDataModule;
using NetPulseApp.Helper;
using NetPulseApp.Forms;
using NetPulseApp.Managers;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages.Settings
{
    /// <summary>
    /// Speed Test — the result-history options TestHistoryStore already reads
    /// (keep history, entry cap), plus clearing saved results.
    /// </summary>
    internal sealed class SpeedTestSettings : SettingsCategory
    {
        public override string Title => "Speed Test";
        public override string Subtitle => "Results history for the Internet Speed Test";

        private static readonly int[] Limits = { 50, 100, 200 };
        private const int DefaultLimit = 200;

        private ToggleSwitchControl _tglKeep;
        private ModernDropDown _cmbLimit;
        private SettingsRowControl _limitRow;
        private Button _btnClear;

        protected override void BuildContent()
        {
            AddSection("History");
            var card = AddCard();

            _tglKeep = new ToggleSwitchControl();
            _tglKeep.CheckedChanged += (s, e) =>
            {
                Commit(c => c.SpeedTestKeepHistory = _tglKeep.Checked);
                _limitRow.SetEnabled(_tglKeep.Checked);
            };
            AddRow(card, "Save test results", _tglKeep,
                "Keep each result so you can compare over time.",
                "history", "results", "save", "keep", "speed");
            card.AddDivider();

            _cmbLimit = MakeDropDown(Limits.Select(l => $"{l} results"), 150);
            _cmbLimit.SelectionChanged += (s, e) =>
            {
                if (IsLoading || _cmbLimit.SelectedIndex < 0) return;
                Commit(c => c.SpeedTestHistoryLimit = Limits[_cmbLimit.SelectedIndex]);
            };
            _limitRow = AddRow(card, "Keep up to", _cmbLimit,
                "Oldest results are removed first.",
                "limit", "maximum", "count", "history");
            card.AddDivider();

            int count = TestHistoryStore.Count();
            _btnClear = MakeButton(ClearLabel(count), (s, e) => ClearResults());
            _btnClear.Enabled = count > 0;
            AddRow(card, "Saved results", _btnClear, null,
                "clear", "delete", "history", "results");

            LoadValues();
        }

        public override void ResetToDefaults()
        {
            Current.SpeedTestKeepHistory = true;
            Current.SpeedTestHistoryLimit = DefaultLimit;
            SettingsService.Save();
        }

        private void LoadValues()
        {
            IsLoading = true;
            try
            {
                _tglKeep.Checked = Current.SpeedTestKeepHistory;
                int index = Array.IndexOf(Limits, Current.SpeedTestHistoryLimit);
                _cmbLimit.SelectedIndex = index >= 0 ? index : Array.IndexOf(Limits, DefaultLimit);
                _limitRow.SetEnabled(_tglKeep.Checked);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static string ClearLabel(int count) => count > 0 ? $"Clear results ({count})" : "No results";

        private void ClearResults()
        {
            if (!AlertNotificationService.Confirm(
                    "This deletes every saved speed test result. This cannot be undone.",
                    "Clear speed test results?",
                    AlertMessageType.Warning,
                    "Clear",
                    "Cancel"))
                return;

            TestHistoryStore.Clear();
            _btnClear.Text = ClearLabel(0);
            _btnClear.Enabled = false;
            ToastsNotificationManager.Instance.ShowSuccessNotification("Speed test results cleared.");
        }
    }
}
