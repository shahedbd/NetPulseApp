# NetPulse — App Requirements: Descriptions & Core Features

**Purpose:** Single consolidated doc for the Microsoft Store listing and product overview — short descriptions, long descriptions, and the complete core feature list (line by line).

**Sources:** `doc/App Identity.md`, `doc/NetPulse_Toolkit_UI_Wireframe.md`, `doc/UI_Implementation_Notes.md`, `doc/MVP_Release_plan/MVP_Feature_Recommendations.md`, `doc/MVP_Release_plan/MVP_P1_Task_Plan.md`, `doc/MVP_Release_plan/MVP_Test_Results.md`, `doc/MVP_Release_plan/Settings_UI_Redesign_Plan.md`, and the current `dev` source (11 built nav pages + About).

**App version covered:** 1.0.0 (MVP) · **Compiled:** 2026-10-28 · **Status:** feature-complete for MVP except F6 (background monitoring — decision pending, see §5)

---

## 1. App identity (context for the listing)

| Field | Value |
|---|---|
| App name | NetPulse |
| Subtitle | Network Diagnostic Toolkit |
| Full store title (per `App Identity.md`) | NetPulse — Network Diagnostic Toolkit |
| SEO-weighted store title option (per MVP doc §6) | NetPulse: Internet Speed Test, Ping & WiFi Analyzer |
| Publisher / developer | Zero Byte |
| Platform | Windows 10 (build 19041+) and Windows 11, desktop (WinForms on .NET 10, MSIX) |
| Price model | Free — every tool free in v1.0 (Pro add-on / IAP deliberately deferred to v1.1, decision D11) |
| Search keywords to request (7 slots) | speed test, ping, wifi analyzer, ip address, dns, network troubleshooter, port scanner |

> Note: `Helper/AppConfig.cs` currently sets `AppName = "Net Pulse"` — the final identity is **NetPulse** (one word) per `App Identity.md`.

---

## 2. App short descriptions

For the Store listing "Short description" field (limit 1,000 chars; recommended ≤ 200 so it never truncates in search results).

### Primary (from `App Identity.md` — 127 chars)

> Diagnose network problems in one click. Ping, traceroute, DNS lookup, port checker, IP & WHOIS tools — all in one clean app.

### Alternates

- **Alt A — search-term weighted** (~134 chars):
  > Speed test, ping, traceroute, DNS, port checker, WHOIS and Wi-Fi signal — one free toolkit that tells you why your internet is slow.
- **Alt B — outcome first** (~130 chars):
  > Find out what's wrong with your internet in one click — a plain-English diagnosis plus ten network tools in one clean, free app.
- **Alt C — minimal enumeration** (~137 chars):
  > All-in-one network toolkit: one-click diagnosis, speed test, ping, traceroute, DNS, port checker, WHOIS, Wi-Fi info, subnet calculator.

---

## 3. App long descriptions

For the Store listing "Description" field (limit 10,000 chars). Variant 1 is the recommended primary.

### Variant 1 — Full listing (recommended)

```text
NetPulse — Network Diagnostic Toolkit

Why is my internet not working? Stop opening five command prompts. Press one button and NetPulse tells you, in plain English, what's wrong and how to fix it.

ONE-CLICK DIAGNOSIS
"Diagnose My Connection" checks your network adapter, router (gateway), DNS and internet connection in about ten seconds, then gives you a clear verdict — for example: "Your Wi-Fi and router are fine, but your DNS server is slow. Switching to 1.1.1.1 or 8.8.8.8 should fix it." Every check is listed with its status, so you can see exactly where the connection breaks.

LIVE DASHBOARD
Your whole connection at a glance: latency, jitter, packet loss, current download and upload speed, and a connection quality grade that tells you what your line is good for — gaming, video calls, 4K streaming. Plus your local IP, gateway, DNS server, public IP with ISP and location, and a badge when a VPN is detected.

INTERNET SPEED TEST
Measure real capacity, not just current traffic: download and upload speed plus ping and jitter, powered by Cloudflare. Results can be saved to history so you can compare over time — and show your ISP the evidence.

WI-FI INFO
See your Wi-Fi the way a pro does: signal strength (RSSI and %), band (2.4 / 5 / 6 GHz), channel and channel width, Wi-Fi generation, link speed and security type — with a live signal graph.

AND THE FULL TOOLKIT
• Ping — continuous or fixed count, live latency graph, min/avg/max, jitter and packet loss
• Traceroute — every hop with hostname, IP and response time
• DNS Lookup — A, AAAA, MX, TXT, CNAME, NS and SOA records via your system resolver, 1.1.1.1, 8.8.8.8 or any server
• Port Checker — TCP port checks with Open / Closed / Filtered verdicts and common-service presets
• WHOIS & IP — registration data for any domain or IP address, plus your own public IP, ISP and location
• Subnet Calculator — CIDR and subnet-mask math for IPv4 and IPv6: network, broadcast, host range and counts

MADE TO BE EASY
Every result can be copied or exported with one click. Dark and light themes, sharp at any DPI, system-tray friendly — and completely free: every tool, no locked features.

WHO IT'S FOR
Gamers watching latency, remote workers on video calls, students learning networking, and IT pros tired of typing ping, tracert, nslookup and whois by hand.

PRIVACY
Lookups go directly from your PC to the host you enter, plus Cloudflare (speed test) and ipwho.is / ipapi.co (public IP and location). NetPulse runs without admin rights.
```

### Variant 2 — Compact listing (~1,100 chars)

```text
NetPulse answers "why is my internet broken?" in one click. Its plain-English diagnosis checks your adapter, router, DNS and internet in about ten seconds and tells you what to fix — no command line, no jargon.

Beyond diagnosis you get a live dashboard (latency, jitter, packet loss, quality grade, public IP, ISP, VPN badge), a Cloudflare-powered speed test with saved history, and full Wi-Fi details (signal, band, channel, security).

The complete toolkit: Ping with live graph • Traceroute • DNS Lookup (A, AAAA, MX, TXT, CNAME, NS, SOA) • Port Checker • WHOIS & public IP • Subnet Calculator for IPv4/IPv6.

Every result copies or exports in one click. Dark and light themes, high-DPI sharp, tray-friendly, no admin rights — and completely free.
```

### Variant 3 — One-paragraph blurb (for partner sites / cross-promotion)

```text
NetPulse is a free Windows network diagnostic toolkit that replaces five command-line tools with one clean app: a one-click plain-English connection diagnosis, live dashboard with quality grade, internet speed test, Wi-Fi signal info, ping with live graph, traceroute, DNS lookup, port checker, WHOIS/public IP, and a subnet calculator. From Zero Byte, the maker of Net Speed Meter Plus and CPU ZX Pro.
```

---

## 4. Core features — line by line

Every line below is one shippable feature as built (or deliberately planned) for v1.0. This is the source of truth for the listing's feature bullets, the About dialog feature list (fix for blocker B3), and release notes.

### 4.1 One-click diagnosis (hero feature — Diagnose page)

- One-click **Diagnose My Connection** button that runs a full connection check in about 10 seconds (7 checks in ~1.1 s on a healthy line).
- Check 1 — network adapter: is the active adapter up and connected.
- Check 2 — valid local IP: detects APIPA / 169.254.x.x "no DHCP" addresses.
- Check 3 — gateway ping: is the router responding and at what latency.
- Check 4 — DNS resolution: compares the system resolver against 1.1.1.1 to spot slow ISP DNS.
- Check 5 — internet ping: reaches 1.1.1.1 / 8.8.8.8 directly by IP.
- Check 6 — HTTPS reachability: confirms the web actually works, not just ICMP.
- Check 7 — captive portal detection via `msftconnecttest.com`.
- Plain-English verdict card with a suggested fix (≈10 verdict rules), e.g. "Your connection looks healthy" or "Your ISP's DNS is slow (380 ms) — switching to 1.1.1.1 should fix it".
- Per-check results table with ✓ OK / ⚠ slow / ✗ fail status and detail values.
- ICMP-blocked fallback: measures TCP 443 connect latency when ping is blocked (corporate / hotel networks), so the verdict never wrongly says "no internet".
- "Open network settings" link to `ms-settings:network-status` (the "Apply DNS" button is deliberately excluded — changing system DNS needs admin; steps are shown instead).

### 4.2 Live dashboard (Dashboard page)

- Live latency tile updated every second from real pings.
- Jitter tile (mean absolute RTT delta).
- Packet loss tile.
- Connection quality score: A–F grade plus a "Good for: Gaming ✓ · Video calls ✓ · 4K streaming ✗" line, computed from latency, jitter and loss.
- Current download / current upload throughput tiles (renamed from raw Mbps; show "Idle" under 0.1 Mbps so the screen never looks dead).
- Connection info: local IP, gateway, DNS server, MAC address.
- Public IP card: IPv4/IPv6, ISP, ASN, city/country — with Copy and Refresh buttons.
- Public-IP provider chain (ipwho.is → ipapi.co → Cloudflare trace) with automatic fallback and a ~5-minute cache (429 rate-limit safe).
- **VPN detected** badge when the active adapter is a TAP/WireGuard/tunnel adapter or the ASN belongs to a known VPN provider.
- Network up/down change events automatically refresh the connection data.

### 4.3 Internet speed test (Speed Test page)

- Download speed test against Cloudflare endpoints.
- Upload speed test against Cloudflare endpoints.
- Ping (latency) and jitter measurement.
- Clear progress phases: ping → download → upload → complete, with a cancellable test (cancel completes in ~2.5 s).
- Full test finishes in about 20–25 s on a normal line.
- Saved results history with a results-per-run list (keep 50 / 100 / 200, configurable).
- Per-result timestamp so speeds can be compared over time.

### 4.4 Wi-Fi info (Wi-Fi page)

- Current SSID and connection state.
- Signal strength as RSSI (dBm) and percentage.
- Live signal-strength history graph (reuses the latency graph control).
- Band detection: 2.4 / 5 / 6 GHz.
- Channel number and channel width (e.g. 20 MHz), computed from the frequency for all three bands.
- Wi-Fi generation / PHY type (Wi-Fi 4 / 5 / 6 / 6E / 7).
- Link (negotiated) speed in Mbps.
- Security type (e.g. WPA2-Personal, WPA3) via the Native WiFi API.
- Graceful "Location permission denied" panel with a Settings link (required on Windows 11 24H2+).
- Graceful "no Wi-Fi adapter" state (Ethernet-only PCs).
- Nearby-networks / channel-congestion analyzer: **not in v1.0** (v1.1).

### 4.5 Ping (Ping page)

- Ping by hostname or IP, with hostname resolution and TTL shown per reply.
- Continuous mode (default) or fixed count: 4 / 10 / 25 / 50 replies.
- Interval presets: 0.5 / 1 / 2 / 5 / 10 seconds (default 1 s).
- Live latency graph where timeouts are drawn as gaps, never zero-spikes; y-axis auto-scales.
- Reply table: SEQ / TIME / TTL / STATUS, color-coded (green reply, red timeout/unknown host, amber unreachable/TTL error).
- Auto-scrolling log capped at 5,000 rows.
- Statistics strip: sent / received / lost (%) / min / avg / max.
- Jitter and quality grade per run.
- Unknown-host and unreachable-IP handled with clear statuses, not silent failures.
- Target history: last 8 targets, deduplicated, most-recent-first, persisted across restarts.
- Enter key in the target box starts the run.

### 4.6 Traceroute (Traceroute page)

- Trace by hostname or IP with configurable maximum hops (default 30).
- Per-hop row: hop number, hostname (reverse DNS), IP, and per-probe RTT.
- Sequential hop numbering with timeout rows (`*`) for non-responding hops.
- Reverse DNS with a 1.5 s per-hop timeout so traces stay fast.
- Unknown-host error surfaced as a message, not an empty table.
- Stop mid-trace supported.

### 4.7 DNS lookup (DNS Lookup page)

- Record types: A, AAAA, MX, TXT, CNAME, NS, SOA, and an "All" mode that dumps every type in one query.
- Resolver selection: system default, 1.1.1.1, 8.8.8.8, or any custom server.
- Result rows: TYPE / NAME / VALUE / TTL.
- Resolver used and query time shown under the results.
- NXDOMAIN handled: "● No records" empty state instead of an error.
- Unreachable custom resolver reported as a clear timeout error row.
- PTR / reverse-lookup record type: **not in v1.0** (v1.1 candidate).

### 4.8 Port checker (Port Checker page)

- TCP port checks against any hostname or IP.
- Port input as a single port, comma list, or range (e.g. `21,22,80,443,3389`).
- "Common ports" preset dropdown so users don't need port numbers by heart (FTP, SSH, HTTP, HTTPS, RDP, and more).
- Status per port: Open / Closed / Filtered, with response time.
- Service-name guess per port (FTP, SSH, HTTP, HTTPS, RDP…).
- Host resolved once up front; unknown host produces one clear error, not per-port failures (fix B2).
- Default timeout 3 s (5 s option) so refused connections report Closed, not Filtered (fix B4 — Windows takes ~2.1 s to report a refusal).
- All ports probed in parallel: a 1,024-port localhost scan completes in under 1 s.
- Scan progress bar with port count.
- Scoped to user-entered hosts and modest concurrency — not marketed as a scanning/hacking tool (Store-policy safe).

### 4.9 WHOIS & public IP (WHOIS IP page)

- WHOIS lookups for any domain via RDAP (rdap.org): registrar, creation date, expiry date, name servers, organization.
- WHOIS for IPv4 addresses (registry data: handle, net name, range…).
- WHOIS for IPv6 addresses (fixed in B3 — 12 fields returned).
- ccTLD support (e.g. `.co.uk` returns full registration data).
- Unregistered domains and bad input return a friendly "No registration data found" message.

### 4.10 Subnet calculator (Subnet Calc page)

- Input as CIDR (`192.168.1.10/24`), dotted mask (`10.0.0.5 255.255.255.0`), or bare IP (treated as /32).
- Outputs: network address, broadcast address, usable host range, total usable hosts, subnet mask, wildcard mask.
- Special cases handled: /31 point-to-point (2 hosts, no broadcast), /0, /32.
- IPv6 support (e.g. `2001:db8::1/64`).
- Address-type labels: RFC 1918 private, documentation ranges.
- Invalid input (/33, non-contiguous masks, garbage) rejected with a clear message.

### 4.11 Shell, settings & cross-cutting

- Left sidebar navigation to all 11 pages, always available mid-diagnosis, collapsible to icons.
- Shell status bar: `● Online · Local IP · Public IP` — a live sanity check that the network is up.
- Dark and light themes, switchable live from Settings (with clickable theme preview cards).
- Searchable, categorized Settings (Appearance, Behavior, Network & IP, Speed Test, Privacy & Data, Reset) with Ctrl+F search, jump-to-row, per-page reset.
- Settings include: preferred public-IP provider, provider fallback, prefer IPv4, tool-history management, speed-test history size, minimize-to-tray, sidebar collapse.
- Copy Results (TSV/clipboard) and Export Report (.txt) on every tool page, each with a toast confirmation.
- Remembered targets across Ping, Traceroute, DNS Lookup, Port Checker and WHOIS (max 8 each), clearable from Settings.
- System tray integration: minimize to tray on close, tray status.
- High-DPI aware: correct at 100 / 150 / 200 % scaling and across mixed-DPI monitors.
- Single-instance guard with a friendly message.
- About dialog with the real feature list and Zero Byte portfolio links (fixes B3).
- No admin rights required for any v1.0 feature.

---

## 5. Out of scope for v1.0 — do NOT claim in the listing

These are planned but not built; listing them would over-promise and risk 1-star reviews:

- Background connection monitoring, outage log and drop/recovery notifications (F6 — build/defer decision open, test finding B5).
- "Start with Windows" startup option (ships with F6; needs an MSIX StartupTask).
- Pro add-on / in-app purchase, ISP Evidence PDF reports, scheduled reports.
- LAN device scanner ("who's on my Wi-Fi"), DNS benchmark, multi-target ping, game-server latency presets, command palette, always-on-top widget.
- Nearby Wi-Fi networks / channel-congestion analyzer.
- PTR / reverse-DNS record type in DNS Lookup.
- Changing system DNS from the app ("Apply suggested DNS" button — needs admin, deliberately excluded).

---

## 6. Release-blocker reminders (from `MVP_Test_Results.md`)

Before any of this text is submitted, the package itself must say NetPulse:

- **B1 (blocker):** `Package.appxmanifest` / `Package.StoreAssociation.xml` still carry the Battery ZX identity — reserve the NetPulse name in Partner Center and re-associate.
- **B6 (review risk):** the startup promotion (opens the CPU ZX Pro Store page and posts device info unrequested) must be disclosed/adjusted — Store policies 10.5.1 / 10.10.
- **B8:** publish the privacy-policy URL covering ipapi.co / ipwho.is / Cloudflare calls and the install telemetry.
