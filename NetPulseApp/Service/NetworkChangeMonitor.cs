// Service/NetworkChangeMonitor.cs
using System.Net.NetworkInformation;
using NetPulseApp.Model;

namespace NetPulseApp.Service
{
    /// <summary>
    /// Tracks the public IP and online state across lookups, and relays the
    /// OS network-change events so pages can refresh without polling.
    /// </summary>
    /// <remarks>
    /// Adapted from IpAddressApp's NetworkChangeMonitor. Pages call
    /// <see cref="Record"/> / <see cref="RecordFailure"/> after each lookup;
    /// the IP-history store and toast notifications are left out here — F6
    /// (background monitoring) subscribes to these events instead.
    /// Events are raised on whatever thread produced them; UI subscribers
    /// must marshal (BeginInvoke) themselves.
    /// </remarks>
    public static class NetworkChangeMonitor
    {
        private static string _lastSeenIp = string.Empty;
        private static bool _wasOnline = true;
        private static bool _hooked;

        /// <summary>An adapter address or availability changed (OS event).</summary>
        public static event Action NetworkChanged;

        /// <summary>Public IP changed from a previously observed value.</summary>
        public static event Action<string> PublicIpChanged;

        /// <summary>Online state flipped — true = restored, false = lost.</summary>
        public static event Action<bool> OnlineChanged;

        /// <summary>A fresh lookup was recorded — lets the status bar and Dashboard share one result.</summary>
        public static event Action<IpInfo> IpRecorded;

        public static bool IsOnline => _wasOnline;

        /// <summary>Hooks the OS network-change events once. Safe to call repeatedly.</summary>
        public static void Start()
        {
            if (_hooked) return;
            _hooked = true;
            NetworkChange.NetworkAddressChanged += (s, e) => NetworkChanged?.Invoke();
            NetworkChange.NetworkAvailabilityChanged += (s, e) => NetworkChanged?.Invoke();
        }

        /// <summary>Records a successful lookup; raises change events when warranted.</summary>
        public static void Record(IpInfo info)
        {
            if (info == null || info.IsCached) return;

            // First lookup of the session establishes the baseline; it is not
            // a "change" worth telling the user about.
            string current = info.PrimaryIp();
            bool isFirstObservation = string.IsNullOrEmpty(_lastSeenIp);

            if (!isFirstObservation && !string.IsNullOrEmpty(current) && current != _lastSeenIp)
                PublicIpChanged?.Invoke(current);

            if (!_wasOnline)
                OnlineChanged?.Invoke(true);

            if (!string.IsNullOrEmpty(current)) _lastSeenIp = current;
            _wasOnline = true;
            IpRecorded?.Invoke(info);
        }

        /// <summary>Records that a lookup failed outright.</summary>
        public static void RecordFailure()
        {
            if (_wasOnline)
                OnlineChanged?.Invoke(false);

            _wasOnline = false;
        }
    }
}
