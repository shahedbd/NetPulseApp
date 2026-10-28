# NetPulse — Settings Tab Redesign Plan

**Pattern source:** `G:\src\MS_Store_App\NetSpeedMeterPlus\NetSpeedMeterPlus\UserControls\Pages\Settings\*`
**Branch:** `dev` · **Written:** 2026-10-28 · **Estimate:** ≈ 1.5–2 working days

Status key: ☐ Todo · ◐ In progress · ☑ Done · ⏸ Blocked / waiting on a decision

---

## 1. Where Settings is today

`UserControls/Pages/SettingsPageControl.cs` is one flat page with three cards:

| Card | Row | Backing field |
|---|---|---|
| Appearance | Dark Mode (toggle) | `ThemeManager.IsDarkTheme` |
| Behavior | Minimize to tray when closing | `MinimizeToTrayOnClose` |
| Data | Tool history → Clear History | `PingTargets`, `TraceTargets`, `DnsTargets`, `PortCheckerHosts`, `WhoisTargets` |

**Gaps**

1. **Saved settings with no UI.** `AppSettingsVm` already stores these, and the services read them, but the user can't change them:
   - `PreferIpv4`, `PreferredProvider`, `ApiAutoFallback` → `Service/IpDataService.cs`
   - `SpeedTestKeepHistory`, `SpeedTestHistoryLimit` → `Helper/TestHistoryStore.cs`
2. **Two settings entry points.** `Forms/SettingsDialog.cs` (opened from the header) duplicates the page and still has a demo "Clear Cache" button that only shows a toast. This is finding B5 in `MVP_Feature_Recommendations.md`.
3. **It won't grow.** F6 (task 6.4) adds monitoring and notification toggles. A single scrolling page of cards gets long and hard to scan.
4. **No search and no per-section reset.**

---

## 2. What we take from NetSpeedMeterPlus

| Piece | NetSpeedMeterPlus file | What it gives us |
|---|---|---|
| Page shell: category column + content host + footer | `SettingsPageControl.cs`, `Settings/SettingsPageControl.Layout.cs` | Left nav with group headings, accent bar on the active link, scrolling content |
| Category base class | `Settings/SettingsCategory.cs` | Title/subtitle, `AddSection`, `AddCard`, `AddRow` (auto-registered for search), `Commit` (write + save), `ResetToDefaults`, `CanReset` |
| Card | `Settings/SettingsCard.cs` | Rows, dividers, full-width children |
| Search | `Settings/SettingsSearchResults.cs` | Ctrl+F, 150 ms debounce, result → jump to row + 1 s highlight flash |
| Theme preview | `Settings/ThemePreviewCard.cs` | Clickable Dark / Light mini-window cards |
| Reset category | `Settings/ResetSettings.cs` | A "Reset all…" row with confirm, in error color |
| `LastState` | `SettingsPageControl.cs` | Reopens on the last category / search |

NetPulse already has `SettingsRowControl`, `SettingsStyle`, `ToggleSwitchControl`, `ModernDropDown`, `AlertNotificationService` and `ToastsNotificationManager`. Only the files in the `Settings/` folder are new.

**One difference from NetSpeedMeterPlus:** there, a theme switch rebuilds the whole page (`NavigationManager.RefreshActivePage`). NetPulse pages re-theme in place through `ThemeManager.ThemeChanged` → `ApplyTheme()`, as the current page does. Keep the NetPulse way: each category gets an `ApplyTheme(AppTheme)` override, and nothing needs `BeginInvoke` tricks to survive its own disposal.

---

## 3. UI layout (ASCII)

### 3.1 Page shell

The app's sidebar stays on the far left. The Settings page splits its own area into a category column and a content pane.

```
┌──────────────┬──────────────────────────────────────────────────────────────────────────┐
│ APP SIDEBAR  │ Settings                                                                 │
│              │ Manage app preferences and tool behavior.                                │
│  Dashboard   ├────────────────────┬─────────────────────────────────────────────────────┤
│  Diagnose    │ ┌────────────────┐ │                                                     │
│  Speed Test  │ │🔍 Search  Ctrl+F│ │  Appearance                                        │
│  Wi-Fi       │ └────────────────┘ │  How the NetPulse window looks                      │
│  Ping        │                    │                                                     │
│  Traceroute  │ APP                │  THEME                                              │
│  DNS Lookup  │ ▌ Appearance       │  ┌───────────────────────────────────────────────┐  │
│  Port Check  │   Behavior         │  │  … category content (see 3.2 – 3.8) …          │  │
│  WHOIS IP    │                    │  └───────────────────────────────────────────────┘  │
│  Subnet Calc │ TOOLS              │                                                     │
│  ──────────  │   Network & IP     │                                                     │
│ ▌Settings    │   Speed Test       │                                                     │
│  About       │   Monitoring       │                                                     │
│              │                    │                                                     │
│              │ DATA               │                                                     │
│              │   Privacy & Data   │                                                     │
│              │   Reset            │                                                     │
│              │                    ├─────────────────────────────────────────────────────┤
│              │                    │  Changes are saved instantly.        [ Reset page ] │
└──────────────┴────────────────────┴─────────────────────────────────────────────────────┘
     200 px          ~190 px                     fills the rest (scrolls)
```

- Category column: about 190 px (a little narrower than NetSpeedMeterPlus's 200, because the app sidebar is also 200). A 1 px `BorderColor` line separates it from the content.
- Group headings are 7.5 pt bold and use `SecondaryTextColor`. Links are 9.5 pt, 34 px high. The active link is bold, uses the `HoverColor` background and has a 3 px `AccentColor` bar (▌).
- Footer: the note on the left; **Reset page** is a ghost button that is hidden on categories with `CanReset = false` (Privacy & Data, Reset) and while search results show.
- The page header (title + subtitle) stays flush under the shell header, like the other tool pages (commit de1e263).

### 3.2 APP → Appearance

```
  Appearance
  How the NetPulse window looks

  THEME
  ┌──────────────────────────────────────────────────────────────┐
  │  ┌──────────────┐   ┌──────────────┐                         │
  │  │▓▓│░░░░░░░░░░│   │▒▒│          │                         │
  │  │▓▓│░░ ░░░░ ░░│   │▒▒│  ▭▭  ▭▭  │                         │
  │  │▓▓│░░░░░░░░░░│   │▒▒│          │                         │
  │  └──────────────┘   └──────────────┘                         │
  │   (●) Dark            ( ) Light                              │
  │ ──────────────────────────────────────────────────────────── │
  │  Same as the theme button in the header.                     │
  └──────────────────────────────────────────────────────────────┘
```

| Row | Control | Field | Reset value |
|---|---|---|---|
| Theme | 2 × `ThemePreviewCard` | `IsDarkMode` via `ThemeManager.SetTheme` | Dark |

### 3.3 APP → Behavior

```
  Behavior
  What the app does when you close or start it

  WINDOW
  ┌──────────────────────────────────────────────────────────────┐
  │  Minimize to tray when closing                       [ ●━ ]  │
  │  Keep running in the system tray after the window closes.    │
  │ ──────────────────────────────────────────────────────────── │
  │  Start with sidebar collapsed                        [ ━○ ]  │
  │  Show only icons in the left navigation.                     │
  └──────────────────────────────────────────────────────────────┘
```

| Row | Control | Field | Reset value |
|---|---|---|---|
| Minimize to tray when closing | Toggle (disabled when `AppConfig.EnableSystemTray` is false) | `MinimizeToTrayOnClose` | On |
| Start with sidebar collapsed | Toggle | `IsSidebarCollapsed` (already saved; also changed by the sidebar's own button) | Off |

> "Start with Windows" is left out on purpose. An MSIX app needs a `StartupTask` declaration in the package manifest, so it belongs with F6 monitoring (see D4).

### 3.4 TOOLS → Network & IP

```
  Network & IP
  How NetPulse finds your public IP and location

  PUBLIC IP LOOKUP
  ┌──────────────────────────────────────────────────────────────┐
  │  Preferred provider                     [ ipwho.is       ▾ ] │
  │  Tried first. ipwho.is and ipapi.co include ISP and city.    │
  │ ──────────────────────────────────────────────────────────── │
  │  Fall back to other providers                        [ ●━ ]  │
  │  If the preferred one fails, try the others.                 │
  │ ──────────────────────────────────────────────────────────── │
  │  Prefer IPv4 address                                 [ ●━ ]  │
  │  Show IPv4 first when you have both IPv4 and IPv6.           │
  └──────────────────────────────────────────────────────────────┘

  TOOL HISTORY
  ┌──────────────────────────────────────────────────────────────┐
  │  Remembered targets          12 saved      [ 🗑 Clear History ]│
  │  Used by Ping, Traceroute, DNS Lookup, Port Checker, WHOIS.  │
  └──────────────────────────────────────────────────────────────┘
```

| Row | Control | Field | Reset value |
|---|---|---|---|
| Preferred provider | `ModernDropDown`: ipwho.is / ipapi.co / Cloudflare (IP + country only) | `PreferredProvider` | ipwho.is |
| Fall back to other providers | Toggle | `ApiAutoFallback` | On |
| Prefer IPv4 address | Toggle | `PreferIpv4` | On |
| Remembered targets | Count label + ghost button with confirm (moved from today's Data card) | the five `*Targets` lists | — (cleared only by the button) |

After a provider change, run `IpDataService.Instance.RefreshAsync()` in the background. `IpDataService` has no cache-invalidate method, so without this the Dashboard's public-IP card would keep the old provider's cached result until the next refresh.

### 3.5 TOOLS → Speed Test

```
  Speed Test
  Results history for the Internet Speed Test

  HISTORY
  ┌──────────────────────────────────────────────────────────────┐
  │  Save test results                                   [ ●━ ]  │
  │  Keep each result so you can compare over time.              │
  │ ──────────────────────────────────────────────────────────── │
  │  Keep up to                             [ 200 results    ▾ ] │
  │  Oldest results are removed first.                           │
  │ ──────────────────────────────────────────────────────────── │
  │  Saved results               37 saved    [ 🗑 Clear Results ] │
  └──────────────────────────────────────────────────────────────┘
```

| Row | Control | Field | Reset value |
|---|---|---|---|
| Save test results | Toggle | `SpeedTestKeepHistory` | On |
| Keep up to | Dropdown: 50 / 100 / 200 (disabled while history is off) | `SpeedTestHistoryLimit` | 200 |
| Saved results | Count + ghost button with confirm | `TestHistoryStore` | — |

### 3.6 TOOLS → Monitoring (lands with F6)

```
  Monitoring
  Watch your connection in the background

  CONNECTION MONITOR
  ┌──────────────────────────────────────────────────────────────┐
  │  Monitor connection in background                    [ ●━ ]  │
  │  Checks your router and the internet every 5 seconds.        │
  │ ──────────────────────────────────────────────────────────── │
  │  Check every                            [ 5 seconds      ▾ ] │
  └──────────────────────────────────────────────────────────────┘

  NOTIFICATIONS
  ┌──────────────────────────────────────────────────────────────┐
  │  Connection lost / restored                          [ ●━ ]  │
  │ ──────────────────────────────────────────────────────────── │
  │  High latency                                        [ ●━ ]  │
  │  Notify when ping stays above        [ 200 ms         ▾ ]    │
  │ ──────────────────────────────────────────────────────────── │
  │  Outage log (last 24 h)        3 events     [ View ] [ Clear ]│
  └──────────────────────────────────────────────────────────────┘
```

New fields (F6): `MonitorEnabled`, `MonitorIntervalSec` (5 / 10 / 30), `NotifyOnDrop`, `NotifyOnHighLatency`, `HighLatencyMs` (100 / 200 / 500). This category is **added to the nav only after F6 ships**. Until then, leave it out of `_factories` instead of showing disabled rows.

### 3.7 DATA → Privacy & Data

```
  Privacy & Data
  What NetPulse stores and sends

  STORED ON THIS PC
  ┌──────────────────────────────────────────────────────────────┐
  │  Settings and history folder               [ 📂 Open folder ] │
  │  %LOCALAPPDATA%\…\NetPulseApp                                │
  └──────────────────────────────────────────────────────────────┘

  SENT FROM THIS PC
  ┌──────────────────────────────────────────────────────────────┐
  │  Network tools contact the hosts you enter, plus ipwho.is,   │
  │  ipapi.co and Cloudflare for public IP and speed tests.      │
  │                                         [ Privacy policy ↗ ] │
  └──────────────────────────────────────────────────────────────┘
```

| Row | Control | Field | Notes |
|---|---|---|---|
| Open folder | Ghost button → `explorer` on the AppData folder | — | Useful for support |
| Privacy note + link | Note + link button | — | Same disclosure the Store listing needs |

`CanReset = false`.

### 3.8 DATA → Reset

```
  Reset
  Start over with the original setup

  RESET
  ┌──────────────────────────────────────────────────────────────┐
  │  Reset all settings                          [ Reset all… ]  │
  │  Theme, tray, sidebar, IP lookup and speed test options go   │
  │  back to defaults. Tool history and test results are kept.   │
  └──────────────────────────────────────────────────────────────┘
```

"Reset all…" uses `ErrorColor` text, asks for confirmation, then calls each category's `ResetToDefaults()` and shows a success toast. It does **not** touch `LaunchCount`, `AppVersion` or the other bookkeeping fields. `CanReset = false`.

### 3.9 Search results

```
  ┌────────────────────┬─────────────────────────────────────────────────────┐
  │ ┌────────────────┐ │  3 results for "ipv"                                │
  │ │🔍 ipv        ✕ │ │                                                     │
  │ └────────────────┘ │  ┌───────────────────────────────────────────────┐  │
  │                    │  │ Prefer IPv4 address          Network & IP  ›  │  │
  │ APP                │  │ Show IPv4 first when you have both…           │  │
  │   Appearance       │  ├───────────────────────────────────────────────┤  │
  │   Behavior         │  │ Preferred provider           Network & IP  ›  │  │
  │ TOOLS              │  │ Tried first. ipwho.is and ipapi.co…           │  │
  │   Network & IP     │  └───────────────────────────────────────────────┘  │
  │   …                │                                                     │
  └────────────────────┴─────────────────────────────────────────────────────┘
       no link is active while results show; Esc clears; click → jump + flash
```

### 3.10 Narrow window (< ~760 px content width)

The category column collapses into a dropdown above the content, so the content keeps at least 320 px:

```
  Settings
  Manage app preferences and tool behavior.
  [ 🔍 Search settings          ]  [ Network & IP        ▾ ]
  ──────────────────────────────────────────────────────────
  PUBLIC IP LOOKUP
  ┌──────────────────────────────────────────────────────┐
  │ …                                                    │
```

---

## 4. File structure

```
UserControls/Pages/
├── SettingsPageControl.cs                 ← rewritten: state, nav, search, theme
└── Settings/                              ← new folder
    ├── SettingsPageControl.Layout.cs      nav column, footer, sizing (port)
    ├── SettingsCategory.cs                base class (port + ApplyTheme)
    ├── SettingsCard.cs                    (port)
    ├── SettingsSearchResults.cs           (port)
    ├── ThemePreviewCard.cs                (port)
    ├── AppearanceSettings.cs
    ├── BehaviorSettings.cs
    ├── NetworkSettings.cs                 Network & IP
    ├── SpeedTestSettings.cs
    ├── MonitoringSettings.cs              added with F6
    ├── PrivacySettings.cs
    └── ResetSettings.cs
Forms/SettingsDialog.cs                    ← removed; header ⚙ navigates to the page
```

Namespace: `NetPulseApp.UserControls.Pages.Settings`. Every row goes through `AddRow(card, title, input, description, keywords…)`, so search works with no extra wiring.

---

## 5. Tasks

| # | Task | Files | Est. | Status |
|---|---|---|---|---|
| S.1 | Port `SettingsCategory`, `SettingsCard`, `SettingsSearchResults`, `ThemePreviewCard`; switch to NetPulse `ThemeManager` / `SettingsService`; add `ApplyTheme` instead of rebuild-on-theme | `Settings/*.cs` | 0.5 d | ☐ |
| S.2 | Rewrite `SettingsPageControl` as the shell: category column, group headings, search (Ctrl+F / Esc), footer with Reset page, `LastState` | `SettingsPageControl.cs`, `Settings/SettingsPageControl.Layout.cs` | 0.5 d | ☐ |
| S.3 | Categories: Appearance, Behavior, Network & IP, Speed Test | `Settings/*Settings.cs` | 0.4 d | ☐ |
| S.4 | Privacy & Data, Reset | `Settings/PrivacySettings.cs`, `ResetSettings.cs` | 0.2 d | ☐ |
| S.5 | Header ⚙ / Settings button → `NavigationManager` opens the Settings page; delete `SettingsDialog` | `HeaderControl.cs`, `HeaderButtonService.cs`, `Forms/SettingsDialog.cs` | 0.1 d | ☐ |
| S.6 | Narrow-window dropdown mode (3.10) | `Settings/SettingsPageControl.Layout.cs` | 0.2 d | ☐ |
| S.7 | QA: dark + light, 100 / 150 / 200 % DPI, search jump, reset per page, settings file survives restart | — | 0.2 d | ☐ |
| 6.4 | (F6) Monitoring category | `Settings/MonitoringSettings.cs` | with F6 | ☐ |

---

## 6. Decisions (yes / no)

| # | Decision | Recommended |
|---|---|---|
| D1 | Replace the flat page with the NetSpeedMeterPlus category layout (column + search + footer)? | **Yes** |
| D2 | Remove `SettingsDialog` and make the header button open the page (fixes B5)? | **Yes** |
| D3 | Add a "Send anonymous install info" opt-out? | **No** — removed from scope (decided 2026-10-28) |
| D4 | Add "Start with Windows" (MSIX `StartupTask`) now? | **No**, add it with F6, where background monitoring makes it useful |
| D5 | Show the Monitoring category before F6 ships, with rows disabled? | **No**, hide it until F6 is built |
| D6 | Group the nav into APP / TOOLS / DATA as shown? | **Yes** |
