using DeviceDataModule;
using NetPulseApp.Model;
using NetPulseApp.Helper;
using NetPulseApp.Service.Ip;

namespace NetPulseApp.Service
{
    /// <summary>
    /// The app's single door to external IP data: provider fallback chain,
    /// dual-stack probe, reverse DNS, and the on-disk cache.
    /// </summary>
    /// <remarks>
    /// Pages never talk to a provider directly. They get one result or one
    /// failure, so a provider outage degrades to a single inline error with a
    /// Retry — never a screen of "N/A" (requirements §3.2).
    /// </remarks>
    public sealed class IpDataService
    {
        /// <summary>
        /// The service every page shares.
        /// </summary>
        /// <remarks>
        /// Each page used to construct its own, so visiting Dashboard, IP
        /// Address, IP Location and Export ran four independent provider chains
        /// and eight address probes within a minute — the fastest way to get
        /// rate-limited by a free tier. One instance means one cache, one
        /// freshness window, and one in-flight request.
        /// </remarks>
        public static IpDataService Instance { get; } = new();

        private static readonly HttpClient Http = CreateClient();

        /// <summary>Guards <see cref="_inFlight"/>.</summary>
        private readonly object _gate = new();

        /// <summary>
        /// The refresh currently running, if any. Concurrent callers await this
        /// rather than starting their own.
        /// </summary>
        private Task<IpInfo> _inFlight;

        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

            // Some free endpoints reject requests without a User-Agent.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"NetPulseApp/{AppConfig.AppVersion} (+{AppConfig.WebsiteUrl})");

            return client;
        }

        /// <summary>Most recent successful result, cached in memory for this session.</summary>
        public IpInfo Current { get; private set; }

        /// <summary>
        /// Returns the cached result from a previous run, if any, so a page can
        /// paint immediately before <see cref="RefreshAsync"/> completes.
        /// </summary>
        public IpInfo LoadCached() => Current ??= IpDataCache.Load();

        /// <summary>
        /// Runs the provider chain and fills in both address families.
        /// </summary>
        /// <returns>
        /// A populated <see cref="IpInfo"/>, or null when every provider failed.
        /// Null means "show the inline error and offer Retry" — callers should
        /// still render whatever <see cref="LoadCached"/> gave them.
        /// </returns>
        /// <param name="force">
        /// Skip the freshness window. Set this for an explicit user Refresh;
        /// leave it false for a page simply coming into view.
        /// </param>
        public async Task<IpInfo> RefreshAsync(
            CancellationToken cancellationToken = default, bool force = false)
        {
            // Recent enough that another round-trip would tell the user nothing
            // new.
            if (!force && Current is { IsCached: false } current &&
                DateTime.UtcNow - current.RetrievedUtc < TimeSpan.FromSeconds(AppConfig.IpFreshnessSeconds))
                return current;

            Task<IpInfo> pending;
            lock (_gate)
            {
                pending = _inFlight is { IsCompleted: false }
                    ? _inFlight
                    : _inFlight = FetchAsync();
            }

            // The shared fetch runs to completion on its own timeouts; this only
            // lets an individual caller stop waiting. Cancelling one page must
            // not abort a lookup the others are still waiting on.
            return await pending.WaitAsync(cancellationToken);
        }

        private async Task<IpInfo> FetchAsync()
        {
            CancellationToken cancellationToken = CancellationToken.None;

            // The dual-stack probe is independent of the geo lookup, so both
            // run together rather than in sequence.
            var probeTask = PublicIpProbe.ProbeAsync(Http, cancellationToken);
            var geoTask = RunChainAsync(cancellationToken);

            await Task.WhenAll(probeTask, geoTask);

            var (ipv4, ipv6) = probeTask.Result;
            var info = geoTask.Result;

            if (info == null)
            {
                // Geolocation failed but the address probe may still have
                // worked — a partial answer beats nothing.
                if (string.IsNullOrEmpty(ipv4) && string.IsNullOrEmpty(ipv6)) return null;

                info = new IpInfo
                {
                    ProviderName = "Direct",
                    RetrievedUtc = DateTime.UtcNow
                };
            }

            // The probe is authoritative for addresses: it pins each family,
            // whereas a provider reports only whichever one it saw.
            if (!string.IsNullOrEmpty(ipv4)) info.PublicIpv4 = ipv4;
            if (!string.IsNullOrEmpty(ipv6)) info.PublicIpv6 = ipv6;

            info.Hostname = await PublicIpProbe.ReverseDnsAsync(
                info.PrimaryIp(SettingsService.Current.PreferIpv4), cancellationToken);

            info.IsCached = false;
            Current = info;
            IpDataCache.Save(info);

            return info;
        }

        /// <summary>
        /// Tries providers in the user's preferred order until one answers.
        /// </summary>
        private async Task<IpInfo> RunChainAsync(CancellationToken cancellationToken)
        {
            foreach (var provider in BuildChain())
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(AppConfig.IpProviderTimeoutSeconds));

                IpInfo result = null;
                try
                {
                    result = await provider.FetchAsync(Http, timeout.Token);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    // This provider timed out; fall through to the next.
                }

                if (result != null) return result;

                // Auto-fallback off means the user asked for one specific
                // source and should be told it failed, not quietly rerouted.
                if (!SettingsService.Current.ApiAutoFallback) break;
            }

            return null;
        }

        /// <summary>
        /// The preferred provider first, then the rest as fallbacks.
        /// </summary>
        private static IEnumerable<IIpProvider> BuildChain()
        {
            var all = new Dictionary<IpProvider, IIpProvider>
            {
                [IpProvider.IpWhoIs] = new IpWhoIsProvider(),
                [IpProvider.IpApiCo] = new IpApiCoProvider(),
                [IpProvider.Cloudflare] = new CloudflareTraceProvider()
            };

            var preferred = SettingsService.Current.PreferredProvider;
            if (all.TryGetValue(preferred, out var first))
                yield return first;

            foreach (var entry in all)
            {
                if (entry.Key != preferred)
                    yield return entry.Value;
            }
        }

        /// <summary>
        /// Cloudflare edge that served this connection, for the Speed Test
        /// server label. Empty when unavailable.
        /// </summary>
        public Task<string> GetTestServerAsync(CancellationToken cancellationToken = default) =>
            CloudflareTraceProvider.GetColoAsync(Http, cancellationToken);
    }
}
