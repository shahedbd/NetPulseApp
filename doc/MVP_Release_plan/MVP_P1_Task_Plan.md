# NetPulse — MVP P1 Task Plan

**Source:** `MVP_Feature_Recommendations.md` §3 (Must-have features, P1)
**Branch:** `dev` · **Updated:** 2026-10-28 · **Target:** ≈ 7–8 working days (down from 10, thanks to reuse)

Status key: ☐ Todo · ◐ In progress · ☑ Done · ⏸ Blocked / waiting on a decision

---

## 1. Status overview

| Feature | Sprint | Est. | Main reuse source | Status |
|---|---|---|---|---|
| F2 Public IP card + status bar | S1 | 0.5 d | IpAddressApp | ◐ Built, awaiting build/QA |
| F5 Jitter + quality score | S1 | 0.5 d | NetPulse `PingService` | ◐ Built, awaiting build/QA |
| F7 Subnet Calculator | S1 | 1 d | — (new, pure math) | ◐ Built, awaiting build/QA |
| F1 Diagnose My Connection ⭐ | S2 | 2–3 d | NetPulse services + IpAddressApp | ◐ Built, awaiting build/QA |
| F3 Internet Speed Test | S3 | 0.5–1 d | WiFiPasswordViewer | ◐ Built, awaiting build/QA |
| F4 Wi-Fi Info / Signal panel | S3 | 1 d | WiFiPasswordViewer | ◐ Built, awaiting build/QA |
| F6 Background monitoring + alerts | S4 | 1.5 d | IpAddressApp + CpuZxPro | ☐ Todo |
| QA pass | S4 | 0.5 d | — | ☐ Todo |

---

## 2. Reuse map (stable code in `G:\src\MS_Store_App`)

| Need | Reuse from | Files | Fit |
|---|---|---|---|
| Public IP, ISP, geo (with fallback + cache) | **IpAddressApp** (Store v1040) | `Service/Ip/*` (`IIpProvider`, `CloudflareTraceProvider`, `IpApiCoProvider`, `IpWhoIsProvider`, `IpDataCache`, `PublicIpProbe`), `Service/IpDataService.cs` | Copy as is; change the namespace |
| VPN detection | IpAddressApp | `Service/Privacy/VpnDetector.cs` | Copy as is |
| Network up/down events | IpAddressApp | `Service/NetworkChangeMonitor.cs` | Copy; drives F2 refresh and F6 |
| Speed test (Cloudflare `__down` / `__up`) | **WiFiPasswordViewer** | `Service/SpeedTestService.cs`, `Model/SpeedTestResult.cs`, `Helper/TestHistoryStore.cs`, `Pages/SpeedTest/*`, `Pages/TestHistory/*` | Copy; restyle with `DataCard` |
| Wi-Fi connection info, signal, Location-denied state | **WiFiPasswordViewer** | `Native/WlanNative.cs`, `Service/WifiAnalyzerService*.cs`, `WifiIeParser.cs`, `WifiSecurityMapper.cs`, `Pages/WifiAnalyzer/{CurrentConnectionTab, SignalHistoryChart, WifiStatePanel}.cs` | Copy; uses the Native WiFi API, not `netsh` |
| Dashboard cards | WiFiPasswordViewer | `UserControls/Lib/DataCard/*` | ☑ Already in NetPulse |
| Popup notifications (no new NuGet) | **CpuZxPro** | `Managers/ToastsNotificationManager.cs`, `AlertNotificationService.cs` | Copy |
| Rating prompt + Pro add-on (IAP) | **DuplicateFinderZx** | `Service/Store/MicrosoftStoreService.cs` (`RequestRateAndReviewAsync`, `GetEntitlementAsync`, `PurchaseProAsync`) | Copy; used after F1/F3 |
| Subnet calculator | — | none found | Write new |

---

## 3. Build order

```
F2 Public IP ──┐
               ├─► F1 Diagnose ──► rating prompt (DuplicateFinderZx Store service)
F5 Jitter ─────┘
F3 Speed Test   (copy, independent)
F4 Wi-Fi panel  (copy, independent)
F7 Subnet Calc  (independent, removes Tab Two)
F6 Tray monitor (after F2 + F5: NetworkChangeMonitor + quality thresholds)
```

| Sprint | Days | Features |
|---|---|---|
| S1 | 1–2 | F2 · F5 · F7 |
| S2 | 3–5 | F1 Diagnose |
| S3 | 6–7 | F3 · F4 (independent, can run in parallel sessions) |
| S4 | 7–8 | F6 · QA |

---

## 4. Task breakdown

**Ground rules:** page = `UserControls/Pages/<Name>Control.cs` + `.Run.cs`; engine = `Service/Network/<Name>Service.cs`; nav only through `AppConfig.NavItems`; new glyphs in `IconChar.cs`; ≤ 300 lines per file. **Done =** builds, dark/light theme, DPI change and minimize/restore all OK, no UI-thread blocking.

### F2 — Public IP card + status bar · S1

| # | Task | Files | Status |
|---|---|---|---|
| 2.1 | Copy the IP provider chain + cache from IpAddressApp | `Service/Ip/*`, `Service/IpDataService.cs` | ☑ |
| 2.2 | Copy `VpnDetector` and `NetworkChangeMonitor` | `Service/Privacy/`, `Service/` | ☑ |
| 2.3 | Dashboard: add Public IP / ISP / Location rows, a VPN badge, Copy and Refresh | `DashboardControl.cs` / `.Run.cs` | ☑ |
| 2.4 | Shell status bar: `● Online · Local IP · Public IP` | `UserControls/Layout/StatusBarControl.cs` (new), `MainForm.cs` | ☑ |

### F5 — Jitter + quality score · S1

| # | Task | Files | Status |
|---|---|---|---|
| 5.1 | Add `Jitter` to `PingStats` (mean absolute RTT delta) | `Service/Network/PingService.cs` | ☑ |
| 5.2 | `QualityScore`: A–F grade + "Good for: Gaming / Calls / 4K" thresholds table | `Service/Network/QualityScore.cs` (new) | ☑ |
| 5.3 | Add a Jitter `DataCard`; show the grade as the Latency card `Badge` | `DashboardControl.cs` / `.Run.cs` | ☑ |
| 5.4 | Rename the Down/Up cards "Current …", and show **Idle** under 0.1 Mbps | `DashboardControl.Run.cs` | ☑ |

### F7 — Subnet Calculator · S1

| # | Task | Files | Status |
|---|---|---|---|
| 7.1 | `SubnetCalculator`: CIDR ↔ mask, network, broadcast, host range/count, wildcard; IPv6 prefix basics | `Service/Network/SubnetCalculator.cs` (new) | ☑ |
| 7.2 | Page: input, results in `ResultsTableControl`, Copy | `UserControls/Pages/SubnetCalcControl.cs` (new) | ☑ |
| 7.3 | Replace the Tab Two nav item and delete `TabTwoControl.cs` | `Helper/AppConfig.cs` | ☑ |

### F1 — Diagnose My Connection ⭐ · S2

| # | Task | Files | Status |
|---|---|---|---|
| 1.1 | `DiagnoseService`: adapter → valid IP → gateway ping → DNS (system vs 1.1.1.1) → internet ping → HTTPS → captive portal; one `CheckResult` event per step | `Service/Network/DiagnoseService.cs` (new) | ☑ |
| 1.2 | ICMP-blocked fallback: TCP 443 connect latency | same | ☑ |
| 1.3 | `DiagnoseVerdicts`: about 10 rules → plain-English verdict + fix steps | `Service/Network/DiagnoseVerdicts.cs` (new) | ☑ |
| 1.4 | Page: Diagnose button, verdict card, check table (✓/⚠/✗), "Open network settings" link | `UserControls/Pages/DiagnoseControl.cs` / `.Run.cs` (new) | ☑ |
| 1.5 | Nav item #2 + Diagnose button on the Dashboard | `AppConfig.NavItems`, `DashboardControl.cs` | ☑ |

### F3 — Internet Speed Test · S3

| # | Task | Files | Status |
|---|---|---|---|
| 3.1 | Copy `SpeedTestService`, `SpeedTestResult` and `TestHistoryStore` from WiFiPasswordViewer | `Service/Network/`, `Model/`, `Helper/` | ☑ |
| 3.2 | Copy the SpeedTest + TestHistory pages; switch the tiles to `DataCard` | `UserControls/Pages/SpeedTestControl*.cs` | ☑ |
| 3.3 | Add the nav item | `AppConfig.NavItems` | ☑ |

### F4 — Wi-Fi Info / Signal panel · S3

| # | Task | Files | Status |
|---|---|---|---|
| 4.1 | Copy `WlanNative`, `WifiAnalyzerService*`, `WifiIeParser`, `WifiSecurityMapper` and the models | `Native/`, `Service/Network/`, `Model/` | ☑ |
| 4.2 | Page from `CurrentConnectionTab` + `SignalHistoryChart` + `WifiStatePanel` (handles Location denied / no adapter) | `UserControls/Pages/WifiControl*.cs` | ☑ |
| 4.3 | Add the nav item; nearby networks/channels tabs stay in v1.1 | `AppConfig.NavItems` | ☑ |

### F6 — Background monitoring + drop alerts · S4

| # | Task | Files | Status |
|---|---|---|---|
| 6.1 | `ConnectionMonitorService`: gateway + 1.1.1.1 every 5 s, plus `NetworkChangeMonitor` events → Online / Degraded / Offline | `Service/Network/ConnectionMonitorService.cs` (new) | ☐ |
| 6.2 | Tray icon tint + copied CpuZxPro toast popup on drop, recovery and high latency (debounced) | `SystemTrayService.cs`, `Managers/ToastsNotificationManager.cs` | ☐ |
| 6.3 | Outage log, 24 h, JSON in AppData | `Service/Network/OutageLog.cs` (new) | ☐ |
| 6.4 | Settings toggles: monitoring, notifications | `SettingsPageControl.cs` | ☐ |

---

## 5. Decisions (yes / no)

| # | Decision | Recommended |
|---|---|---|
| D1 | Copy the IP provider chain + `VpnDetector` from IpAddressApp instead of writing new code? | **Yes** |
| D2 | Copy the speed test from WiFiPasswordViewer (Cloudflare endpoints)? | **Yes** |
| D3 | Copy the Wi-Fi service + current-connection view from WiFiPasswordViewer (Native WiFi API, not `netsh`)? | **Yes** |
| D4 | Include nearby networks / channel analyzer in MVP? | **No** (v1.1; it overlaps WiFiPasswordViewer) |
| D5 | Use the CpuZxPro popup toasts instead of adding a Windows toast NuGet? | **Yes** |
| D6 | Keep the header system summary (CPU/RAM) and put network context in the new status bar instead? | **Yes** |
| D7 | "Apply suggested DNS" button in Diagnose (needs admin)? | **No** (show steps only) |
| D8 | Keep monitoring running when the window is closed to the tray? | **Yes** |
| D9 | Start with Windows by default? | **No** (opt-in after monitoring is turned on) |
| D10 | Ship the rating prompt in MVP (after a successful Diagnose or Speed Test)? | **Yes** (reuse DuplicateFinderZx `MicrosoftStoreService`) |
| D11 | Ship the Pro add-on / IAP gating in MVP? | **No** (v1.1; all P1 features free) |
| D12 | Run S3 (F3 + F4) in parallel sessions? | **Yes** (no shared files) |

---

## 6. QA checklist (end of S4)

| Check | Status |
|---|---|
| Every page: dark/light, 100/150/200 % DPI, minimize → restore from the taskbar | ☐ |
| No network: app launches, shows Offline, Diagnose verdict is correct | ☐ |
| ICMP blocked / captive portal / VPN on | ☐ |
| IP API rate-limited (429) → cached or fallback provider, no crash | ☐ |
| Wi-Fi: Location denied, no Wi-Fi adapter (Ethernet only) | ☐ |
| Monitor runs 1 h minimized: no memory growth, debounced alerts | ☐ |
| Release blockers B1–B8 cleared; Store package builds | ☐ |
