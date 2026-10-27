// UserControls/Pages/DiagnoseControl.Run.cs
using System.Diagnostics;
using System.Text;
using NetPulseApp.Service.Network;
using NetPulseApp.UiComponent;
using NetPulseApp.UserControls.Lib;

namespace NetPulseApp.UserControls.Pages
{
    /// <summary>
    /// Diagnose page run logic: button wiring, run lifecycle, one table row
    /// per CheckResult, the verdict card, and report building. Layout/theme
    /// live in DiagnoseControl.cs.
    /// </summary>
    public partial class DiagnoseControl
    {
        private Verdict _verdict;

        private void WireService()
        {
            _service.CheckCompleted += OnCheck;
            _service.RunFinished += OnFinished;
        }

        private void WireButtons()
        {
            _diagnose.Click += (s, e) => StartDiagnose();
            _copy.Click += (s, e) => ToolUiFactory.CopyResults(BuildReport());
            _export.Click += (s, e) => ToolUiFactory.ExportReport(FindForm(),
                $"NetPulse_Diagnose_{DateTime.Now:yyyyMMdd_HHmm}.txt", BuildReport());
            _openSettings.LinkClicked += (s, e) => OpenNetworkSettings();
        }

        // ─────────────────────────────────────────────────────────────────────
        // RUN CONTROL
        // ─────────────────────────────────────────────────────────────────────

        private void StartDiagnose()
        {
            if (IsDisposed || _service.IsRunning) return;

            _verdict = null;
            _table.ClearRows();
            _diagnose.Enabled = false;
            _diagnose.Text = "Checking…";
            ShowVerdictText("Checking your connection…", "Running 7 checks, from your adapter out to the internet.", "");
            _service.Start();
        }

        private void OnCheck(CheckResult check)
        {
            if (IsDisposed) return;
            _table.AddRow(new TableRow(new[]
            {
                check.Name,
                StatusText(check.Status),
                check.LatencyMs.HasValue ? $"{check.LatencyMs} ms" : "—",
                check.Detail
            }, accentColumn: 1, accent: StatusColor(check.Status)));
        }

        private void OnFinished(DiagnoseReport report)
        {
            if (IsDisposed) return;
            _diagnose.Enabled = true;
            _diagnose.Text = "Run Again";

            _verdict = DiagnoseVerdicts.Evaluate(report);
            string steps = _verdict.FixSteps.Length == 0 ? ""
                : "Try this:\n" + string.Join("\n", _verdict.FixSteps.Select((s, i) => $"  {i + 1}. {s}"));
            ShowVerdictText(_verdict.Title, _verdict.Explanation, steps);
        }

        private void ShowVerdictText(string title, string text, string steps)
        {
            _verdictTitle.Text = title;
            _verdictText.Text = text;
            _verdictSteps.Text = steps;
            _verdictSteps.Visible = steps.Length > 0;
            ApplyVerdictTheme();
            LayoutVerdict();
        }

        private void ApplyVerdictTheme()
        {
            var theme = themeManager.CurrentTheme;
            _verdictTitle.ForeColor = _verdict?.Severity switch
            {
                VerdictSeverity.Good => theme.SuccessColor,
                VerdictSeverity.Warning => theme.WarningColor,
                VerdictSeverity.Problem => theme.ErrorColor,
                _ => theme.HeadingColor
            };
        }

        private static string StatusText(CheckStatus status) => status switch
        {
            CheckStatus.Pass => "✓ Pass",
            CheckStatus.Warn => "⚠ Warning",
            CheckStatus.Fail => "✗ Fail",
            _ => "– Skipped"
        };

        private Color StatusColor(CheckStatus status)
        {
            var theme = themeManager.CurrentTheme;
            return status switch
            {
                CheckStatus.Pass => theme.SuccessColor,
                CheckStatus.Warn => theme.WarningColor,
                CheckStatus.Fail => theme.ErrorColor,
                _ => theme.SecondaryTextColor
            };
        }

        private static void OpenNetworkSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:network") { UseShellExecute = true });
            }
            catch { /* Settings app unavailable — nothing useful to report */ }
        }

        private string BuildReport()
        {
            if (_table.RowCount == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine($"NetPulse — Diagnose My Connection ({DateTime.Now:yyyy-MM-dd HH:mm})");
            if (_verdict != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Verdict: {_verdict.Title}");
                sb.AppendLine(_verdict.Explanation);
                foreach (var (step, i) in _verdict.FixSteps.Select((s, i) => (s, i)))
                    sb.AppendLine($"  {i + 1}. {step}");
            }
            sb.AppendLine();
            sb.Append(_table.GetAsText());
            return sb.ToString();
        }
    }
}
