# NetPulse — MVP Feature Recommendations

**Goal:** Get as many Microsoft Store installs as possible, and keep users around, before and right after MVP release.
**Inputs:** `NetPulse_Toolkit_UI_Wireframe.md`, `UI_Implementation_Notes.md`, `App Identity.md`, the current source (`dev` @ 87f9039) and the live Dashboard screenshot.
**Date:** 2026-10-25

---

## 1. Where the app stands today

| Area | State |
|---|---|
| Dashboard (live latency, down/up Mbps, packet loss, connection info) | ✅ Built — looks good |
| Ping, Traceroute, DNS Lookup, Port Checker, WHOIS (RDAP) | ✅ Built, shared table/graph/export pattern |
| Settings, dark/light theme, tray, DPI | ✅ Built (from the template) |
| **Diagnose My Connection** (the wireframe's main selling point) | ❌ Not built |
| **Public IP / ISP / location card** (wireframe WHOIS screen) | ❌ Not built — WHOIS page only does lookups |
| Subnet Calculator | ❌ Not built |
| Status bar (Online · Local IP · Public IP) | ❌ Not built |
| Pro / upgrade path | ❌ Not built |

The toolkit works, but right now it is "five cmd.exe commands with a nicer UI". Many free apps already do that. The wireframe's main idea was **"answer *why is my internet broken?* in one click"**, and that piece is still missing. It is also the feature most likely to bring in installs.

---

## 2. Release blockers (P0, must fix before you submit)

These are not features. Any one of them can get the app rejected, or earn 1-star reviews in its first week.

| # | Issue | Where | Fix |
|---|---|---|---|
| B1 | Package identity is **Battery ZX** (`Name="4789ZeroByte.BatteryZX"`, DisplayName, Description) | `WinAppPakaging/Package.appxmanifest` | Reserve the "NetPulse" name in Partner Center and associate the package with it (Package.StoreAssociation.xml too) |
| B2 | Title bar shows **"Desktop App Template — System Information"** | `Helper/AppConfig.cs` `AppName`, `AppSubtitle` | `NetPulse` / `Network Diagnostic Toolkit` (per `App Identity.md`) |
| B3 | About dialog lists **"Test data 01–04"**, "Your Company Name", example.com, support@example.com | `AppConfig.AboutFeatures`, `DeveloperName`, `WebsiteUrl`, `SupportEmail` | Real feature list, Zero Byte, real site and support email |
| B4 | **"Tab Two"** placeholder in the nav | `AppConfig.NavItems`, `TabTwoControl.cs` | Remove it, or replace it with the Subnet Calculator (§3) |
| B5 | Header **"Settings" button and the ⚙ icon both open Settings**, and Settings is also in the nav | Header | Keep one entry point; use the header slot for **"Diagnose"** or a **public-IP chip** |
| B6 | Unused `System.Windows.Forms.DataVisualization` package | csproj | Remove it (smaller package, faster cold start) |
| B7 | Dashboard header shows CPU/RAM ("AMD Ryzen 7… 31.4 GB RAM") | Header | Show network context instead: `● Online · Wi-Fi "HomeNet" · 192.168.0.102 · Public 203.0.113.7` |
| B8 | Privacy policy URL is required, because the app calls ipapi.co, rdap.org and the install-tracking API | Store listing | Publish a short privacy page; disclose the device-install telemetry |

---

## 3. Must-have features for MVP (P1)

Ranked by **install impact ÷ effort**. Items 1–4 fill gaps the app's own wireframe already promised.

### 1. Diagnose My Connection ⭐ (the hero feature)
- **Why:** Store search traffic comes from people whose internet just broke ("internet not working", "wifi slow", "network troubleshooter"). Windows' own troubleshooter is widely disliked. A one-click button that gives a plain-English answer is what sets NetPulse apart, and it makes a strong first screenshot for the listing.
- **What:** A large `Diagnose` button on the Dashboard. It checks, in order: adapter up → has IP (not 169.254.x) → gateway ping → DNS resolve (system + 1.1.1.1 for comparison) → internet ping (1.1.1.1 / 8.8.8.8) → HTTPS reachability → captive-portal check (`http://www.msftconnecttest.com/connecttest.txt`).
- **Output:** One verdict card ("Your Wi-Fi and router are fine, but your ISP's DNS is slow (380 ms). Switching to 1.1.1.1 should fix it."), plus the check table from the wireframe.
- **Effort:** Low–medium. `PingService`, `DnsLookupService` and `DashboardService` already give you every signal; this is orchestration plus a rules table of about 10 verdicts.
- **Skip for MVP:** the wireframe's "Apply Suggested DNS" button. It needs admin rights and changes system settings. Show the steps instead (or open `ms-settings:network-status`).

### 2. "What's my IP" card + status bar
- **Why:** "What is my IP" is one of the most-searched network queries. Having it on screen the moment the app opens gives users value within 2 seconds of the first launch.
- **What:** Public IPv4/IPv6, ISP, ASN, city/country (ipapi.co, already a dependency), with **Copy** and **Refresh**. Show it on the Dashboard connection card and in the shell status bar. Add a **VPN detected** badge when the active adapter is a TAP/WireGuard/tunnel adapter, or when the ISP/ASN belongs to a known VPN provider.

### 3. Internet Speed Test
- **Why:** "speed test" is a far larger search term than "ping" or "traceroute". The Dashboard already shows Mbps, but that is *current traffic*, not *capacity*, and users will read "0.0 Mbps" as "broken". A real speed test answers the question most users came with.
- **What:** Download/upload/latency/jitter against a CDN endpoint (for example, Cloudflare's speed endpoints), with a big gauge and a history list. You may already have this code from **Net Speed Plus / Battery ZX**; reuse it.
- **Also:** Relabel the Dashboard tiles to "Current download" and "Current upload", or show "idle" when traffic is under 0.1 Mbps, so the screen never looks dead.

### 4. Wi-Fi Info / Signal panel
- **Why:** Most home users' problems are Wi-Fi problems. "WiFi analyzer" is a strong Store category, and the app currently says only "Wi-Fi".
- **What:** SSID, signal % / RSSI, band (2.4/5/6 GHz), channel, link speed, security type, and a live signal graph (reuse `LatencyGraphControl`). Data comes from `netsh wlan show interfaces` parsing or the Native WiFi API. A nearby-networks / channel-congestion list can come in v1.1.
- **Note:** On Windows 11 24H2+, Wi-Fi scan data requires Location permission. Detect the "denied" case and show a hint instead of empty fields.

### 5. Connection quality score + jitter
- **Why:** Gamers and remote workers search for "jitter test", "packet loss test" and "lag". One clear score is easy to share and screenshot.
- **What:** Add a **Jitter** tile next to Latency, and an overall grade: **A–F** or **"Good for: Gaming ✓ · Video calls ✓ · 4K streaming ✗"**, computed from latency, jitter and loss. It costs almost nothing, because `PingService` stats already exist.

### 6. Background monitoring + drop alerts (tray)
- **Why:** This keeps users coming back. An app that warns "your internet dropped at 2:14 AM for 3 minutes" stays installed, and those users leave reviews. The tray infrastructure (`SystemTrayService`, `MinimizeToTrayOnClose`) already exists.
- **What:** When minimized, keep pinging the gateway and 1.1.1.1 every few seconds. Change the tray icon colour (green/amber/red) and show a Windows notification on disconnect, reconnect and high latency. Keep an **Outage Log** (start, end, duration, which hop failed).
- **Free vs Pro:** The live tray status and notifications are free. The long-term log history and export are Pro (§5).

### 7. Subnet Calculator
- Already in the wireframe and it is pure math, so it is about one day of work. It removes "Tab Two" and widens the IT and student audience. Include CIDR ↔ mask, host range, and IPv6 basics.

---

## 4. Lucrative / differentiating features (P2: MVP if time allows, otherwise v1.1)

| Feature | Why it brings installs or money | Effort |
|---|---|---|
| **LAN Device Scanner** ("Who's on my Wi-Fi?") | Huge consumer search demand: ARP + ping sweep, hostname, MAC vendor, "new device joined" alert. Highly shareable | Medium |
| **ISP Report / Evidence PDF** | "Prove to your ISP that the connection is bad": a 24h log of latency, loss and outages exported as a clean PDF. Strong reason to buy Pro | Medium (needs monitoring #6) |
| **Website Up/Down Checker + SSL expiry** | "Is it down for everyone or just me?" Pairs HTTP status, response time and certificate expiry. Popular with IT and devs | Low |
| **Wake-on-LAN** | Small, much-loved power-user tool; adds a keyword to the listing | Low |
| **Public DNS Benchmark** | Ranks Cloudflare, Google, Quad9, OpenDNS and ISP DNS by speed, and feeds the Diagnose verdict | Low |
| **Multi-target ping (ping plotter style)** | Gamers ping several game servers at once; PingPlotter-style views sell for $$ | Medium |
| **Game server latency presets** | One-click ping to Valorant, CS2, Fortnite, LoL, Steam regions. Reaches gaming search traffic | Low (just lists of hosts) |
| **Command palette / quick target bar** | Type `ping google.com`, `dns example.com mx`, `port 1.2.3.4:443`. Power users love it; good for "best of" reviews | Low–medium |
| **Compact always-on-top widget** | Mini latency/speed overlay (your Net Speed Meter users already know this pattern). Strong retention | Medium |

---

## 5. Monetization: Free vs Pro

Keep **every diagnostic tool free**. That gets the installs, ratings and search ranking. Charge for **time** (monitoring, history) and **proof** (reports).

| Free | Pro (one-time ≈ $4.99–6.99, or ≈ $1.49/mo) |
|---|---|
| All tools: Diagnose, Ping, Traceroute, DNS, Port, WHOIS, IP, Speed Test, Wi-Fi info, Subnet | Background monitoring with **unlimited** outage history (free: last 24 h) |
| Live dashboard + tray status + drop notifications | **ISP Evidence PDF** reports and scheduled reports |
| Copy / TXT export | CSV/JSON export, multi-target ping, LAN scanner alerts, compact widget |
| | Custom themes / accent colours (the template already has a colour picker) |

- Use **Store add-on (durable IAP)** via `Windows.Services.Store`. Don't block anything with nag dialogs; show a single "Pro" badge on gated controls.
- Cross-promote the Zero Byte portfolio (Net Speed Plus, CPU ZX links are already in `AppConfig`) from About, not as pop-ups.

---

## 6. Install-growth levers (non-code but critical)

1. **Store title is your SEO.** Keep "NetPulse —" and put the searched words in the title: e.g. *"NetPulse: Internet Speed Test, Ping & WiFi Analyzer"*. Microsoft Store search weighs the title heavily. Use the 7 search-term slots for: *speed test, ping, wifi analyzer, ip address, dns, network troubleshooter, port scanner*.
2. **Screenshots in order:** (1) Diagnose verdict "Your DNS is slow — here's the fix", (2) Speed test gauge, (3) Dashboard live graph, (4) Wi-Fi signal, (5) Tools grid. Add a caption bar to each, in dark theme.
3. **Rating prompt:** Use the existing `LaunchCount` / `PromotionCount` in `AppSettingsVm`. Ask with `StoreContext.RequestRateAndReviewAppAsync()` **only after a successful Diagnose or Speed Test**, never on launch. That one trigger usually lifts ratings noticeably.
4. **First-run experience:** Open on the Dashboard with the Diagnose button and the public IP already filled in. Show no empty states on first launch.
5. **Localization:** Even machine-translated listing text (ES, PT-BR, DE, FR, ID, TR, RU, JA) multiplies the Store markets you appear in. UI strings can follow in v1.1.
6. **Jump list / tray menu shortcuts:** "Run Diagnose", "Speed Test", "Copy Public IP" keep the app in daily use.
7. **Startup option:** "Start with Windows (minimized to tray)", off by default, offered after the user turns on monitoring. `StartupService` already exists.

---

## 7. Polish items seen in the screenshot

- The **Pause** button looks disabled (grey on dark). Give it an outline or accent style like the tool Start/Stop buttons.
- The graph's x-axis runs to 18 s while the line stops at 15 s, which leaves dead space. Let the axis follow the data, or use a fixed 60 s rolling window.
- The "28 ms" floating label over the graph duplicates the Latency tile. Show the current point's tooltip or the min/avg instead.
- The Upload tile colour (indigo) is close to the Latency blue. Use a more distinct hue.
- Add **hover tooltips** on tiles (e.g. "Packet loss over the last 8 pings").
- Make the connection rows (IP, Gateway, DNS, MAC) **click-to-copy**.
- Add keyboard shortcuts: `Ctrl+1…7` for tools, `F5` to re-run, `Ctrl+Shift+C` to copy results.

---

## 8. Recommended MVP scope & order

| Order | Item | Est. |
|---|---|---|
| 1 | Release blockers B1–B8 | 0.5–1 day |
| 2 | Public IP card + status bar (#2) | 1 day |
| 3 | **Diagnose My Connection** (#1) | 2–3 days |
| 4 | Speed Test (#3, reuse existing code) | 1–2 days |
| 5 | Jitter + quality score (#5) | 0.5 day |
| 6 | Wi-Fi panel (#4) | 1–2 days |
| 7 | Subnet Calculator (#7), replacing Tab Two | 1 day |
| 8 | Tray monitoring + drop notifications (#6, free tier) | 2 days |
| 9 | Rating prompt + Store listing/SEO (§6) | 0.5 day |
| — | **MVP ship** | **≈ 2 weeks** |
| v1.1 | Pro add-on, ISP Evidence PDF, LAN scanner, DNS benchmark, game presets, widget | — |

**Recommended navigation after MVP:**
`Dashboard · Diagnose · Speed Test · Wi-Fi · Ping · Traceroute · DNS · Port Checker · IP & WHOIS · Subnet Calc · ── · Settings · About`

---

## 9. Risks / notes

- **Port scanning:** Keep it scoped to user-entered hosts and a modest concurrency. Aggressive scanning can trigger Defender or Store policy flags. Don't market the app as a "hacking" tool.
- **ICMP blocked networks:** Some corporate and hotel networks block ping. Diagnose should fall back to TCP-connect latency (port 443) so the verdict isn't "no internet".
- **Third-party APIs** (ipapi.co, rdap.org): rate limits on the free tiers. Cache public-IP results for about 5 minutes and handle 429 errors gracefully.
- **Privacy:** Disclose the installation-info telemetry (`apiUrlProd`) in the privacy policy. Consider an opt-out toggle in Settings; reviewers and privacy-minded users notice this.
