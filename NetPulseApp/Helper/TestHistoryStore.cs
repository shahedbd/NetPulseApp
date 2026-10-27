using System.Text.Json;
using DeviceDataModule;
using NetPulseApp.Model;

namespace NetPulseApp.Helper
{
    /// <summary>
    /// Local JSON log of completed speed tests. Append-only history (not a
    /// TTL cache) — newest entry first, capped at MaxEntries so the file
    /// doesn't grow unbounded (no pagination in the Test History table).
    /// Whether results are kept, and the cap (50/100/200), come from
    /// Settings › Speed Test &amp; Tools.
    /// </summary>
    public static class TestHistoryStore
    {
        private static readonly string appDataDir = ApplicationDataManager.Instance.ApplicationDataDirectory;

        private static readonly string HistoryFile =
            Path.Combine(appDataDir, "TestHistory.json");

        private static int MaxEntries => SettingsService.Current.SpeedTestHistoryLimit;

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        // ─────────────────────────────────────────────────────────────────────
        // LOAD  — newest first
        // ─────────────────────────────────────────────────────────────────────

        public static List<SpeedTestResult> Load()
        {
            try
            {
                if (!File.Exists(HistoryFile))
                    return new List<SpeedTestResult>();

                string json = File.ReadAllText(HistoryFile);
                return JsonSerializer.Deserialize<List<SpeedTestResult>>(json, Options)
                    ?? new List<SpeedTestResult>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load test history: {ex.Message}");
                return new List<SpeedTestResult>();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // APPEND  — call after a successful completed test
        // ─────────────────────────────────────────────────────────────────────

        public static void Append(SpeedTestResult result)
        {
            if (!SettingsService.Current.SpeedTestKeepHistory) return;
            try
            {
                var history = Load();
                history.Insert(0, result);

                if (history.Count > MaxEntries)
                    history.RemoveRange(MaxEntries, history.Count - MaxEntries);

                string json = JsonSerializer.Serialize(history, Options);
                AtomicFile.WriteAllText(HistoryFile, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to save test history: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CLEAR
        // ─────────────────────────────────────────────────────────────────────

        public static int Count() => Load().Count;

        public static void Clear()
        {
            try
            {
                if (File.Exists(HistoryFile))
                    File.Delete(HistoryFile);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to clear test history: {ex.Message}");
            }
        }
    }
}
