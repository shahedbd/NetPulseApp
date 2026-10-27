using System.Text.Json;
using DeviceDataModule;
using NetPulseApp.Model;

namespace NetPulseApp.Service.Ip
{
    /// <summary>
    /// Last successful IP lookup, persisted to AppData.
    /// </summary>
    /// <remarks>
    /// Serves two requirements. It lets the Dashboard paint a real address
    /// immediately on launch instead of a spinner, and it is the only thing the
    /// app can honestly show when offline — labelled with its age, never passed
    /// off as live (see <see cref="IpInfo.IsCached"/>).
    /// <para>
    /// Same JSON-in-AppData shape as the other AppData JSON stores.
    /// </para>
    /// </remarks>
    public static class IpDataCache
    {
        private static readonly string CacheFile = Path.Combine(
            ApplicationDataManager.Instance.CacheDirectory, "LastIpInfo.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Reads the cached result, flagged as cached. Returns null when there
        /// is nothing usable on disk.
        /// </summary>
        public static IpInfo Load()
        {
            try
            {
                if (!File.Exists(CacheFile)) return null;

                var info = JsonSerializer.Deserialize<IpInfo>(File.ReadAllText(CacheFile), Options);
                if (info == null || info.IsEmpty) return null;

                info.IsCached = true;
                return info;
            }
            catch
            {
                // A corrupt cache is not worth surfacing — it just means the
                // app starts with no head start.
                return null;
            }
        }

        public static void Save(IpInfo info)
        {
            if (info == null || info.IsEmpty) return;

            try
            {
                File.WriteAllText(CacheFile, JsonSerializer.Serialize(info, Options));
            }
            catch
            {
                // Caching is an optimisation; failing to write must never break
                // a lookup that already succeeded.
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(CacheFile)) File.Delete(CacheFile);
            }
            catch { }
        }

        /// <summary>
        /// Human-readable age for the cached-value label, e.g. "3 minutes ago".
        /// </summary>
        public static string DescribeAge(DateTime retrievedUtc)
        {
            var age = DateTime.UtcNow - retrievedUtc;

            if (age < TimeSpan.Zero) return "just now";
            if (age.TotalSeconds < 60) return "just now";
            if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} min ago";
            if (age.TotalHours < 24) return $"{(int)age.TotalHours} h ago";

            return retrievedUtc.ToLocalTime().ToString("MMM dd, HH:mm");
        }
    }
}
