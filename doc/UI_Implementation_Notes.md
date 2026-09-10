# NetPulse Toolkit — UI Implementation Notes

Living log for the screen-by-screen UI implementation from
`doc/NetPulse_Toolkit_UI_Images`. Order: 02 Ping → 03 Traceroute → 04 DNS →
05 Port Checker → 06 WHOIS/IP → 07 Settings → 01 Dashboard (last).

## Screen status

| # | Screen | Status |
|---|--------|--------|
| 02 | Ping | ✅ Done |
| 03 | Traceroute | ⬜ next |
| 04 | DNS Lookup | ⬜ |
| 05 | Port Checker | ⬜ |
| 06 | WHOIS / IP | ⬜ |
| 07 | Settings | ⬜ |
| 01 | Dashboard (Running) | ⬜ last |

## Shared components (built with Ping, reused by later screens)

All tool pages follow the template's pattern: page = `UserControls/Pages/*`
wired through `AppConfig.NavItems`; theme re-applied by subscribing to
`ThemeManager.ThemeChanged` and unsubscribing in `Dispose` (pages are disposed
on every nav switch).

- **`UiComponent/ToolUiFactory.cs`** — labels (page title/subtitle/caption),
  themed inputs (`Input`, `EditableCombo`, `ApplyInputTheme`), buttons
  (`AccentButton` green Start / red Stop, `GhostButton` Export/Copy +
  `ApplyAccentTheme`/`ApplyGhostTheme`), cards, shared footer actions
  (`CopyResults`, `ExportReport` — clipboard/SaveFileDialog + toast), and
  `LabeledValue` combo item.
- **`UserControls/Lib/ResultsTableControl.cs`** — themed owner-drawn virtual
  ListView, the shared results grid for Ping/Traceroute/DNS/Port.
  `SetColumns(("SEQ",64),("STATUS",0))` (width 0 = stretch), `AddRow(TableRow)`
  with optional accent color on one column, `GetAsText()` (TSV), auto-scroll,
  5000-row cap. Row height + column widths are applied on `HandleCreated`
  from the control's real `DeviceDpi` (neither scales via AutoScaleMode.Dpi,
  and the control is built before parenting). Call `ApplyTheme()` on theme
  change.
- **`UserControls/Lib/LatencyGraphControl.cs`** — live latency graph card,
  **custom GDI+** (gridlines, filled line, live dot) drawn from AppTheme's
  Graph* colors incl. `GraphFillColor`. The stock
  `System.Windows.Forms.DataVisualization` Chart rendered blank and crashed
  the app on this runtime, so it was dropped — the package is now unused and
  can be removed from the csproj. `AddPoint(elapsedSec, ms)` (null ms = gap
  for timeout), `Clear()`, `SetLive(bool)`, `ApplyTheme()`. Timeouts are
  gaps, never zero-spikes; y-axis grows via `NiceCeiling`.
- **`Service/Network/PingService.cs`** — ICMP loop (`System.Net.NetworkInformation.Ping`,
  fresh instance per attempt since `SendPingAsync` isn't cancellable). Events
  marshalled to the UI thread via `SynchronizationContext` captured at
  `Start(host, intervalMs, count: 0=continuous)`: `ReplyReceived(result, stats)`,
  `RunError`, `RunFinished`.

## 02 Ping — details

- Page: `UserControls/Pages/PingControl.cs`; nav entry added in
  `AppConfig.NavItems` (green `SatelliteDish` icon); `TabOneControl.cs`
  placeholder deleted (Tab Two placeholder remains until Traceroute).
- New `IconChar` glyphs: `Copy`, `Download`, `Play`, `Stop`, `SatelliteDish`.
- Target history (max 8, deduped, most-recent-first) persisted via the
  existing `SettingsService` — new `AppSettingsVm.PingTargets` field.
- Interval presets 0.5/1/2/5/10 s (default 1 s); Continuous (default) or
  Count 4/10/25/50; Enter in the target box starts a run.
- Reply log rows: SEQ / TIME / TTL / STATUS with STATUS accented
  (green Reply, red Timeout/Unknown host, amber unreachable/TTL errors).
- Stats strip: `Sent / Received / Lost (%) / Min/Avg/Max`.
- Footer: Copy Results (TSV) and Export Report (.txt via SaveFileDialog),
  both with toast confirmations; empty-state info toasts.

### Caveats / follow-ups

- **DPI model (important for all pages):** `DpiAwareService.Scale()` is a
  deliberate pass-through at runtime (see its `ScaleFactor` comment) — pages
  extend `DpiAwareUserControl` and write base-pixel values; WinForms'
  `AutoScaleMode.Dpi` does the actual scaling. Controls that need
  non-auto-scaled metrics (ListView row heights, column widths) must read
  their own `DeviceDpi` on `HandleCreated`.
- **Docking order:** WinForms claims dock edges from the last-added control
  first. Page build order is: Fill (table) → Bottom (stats, then footer) →
  Top bottom-up (graph, input, header) so the header lands topmost.
- At the 1000 px minimum window width the right end of the input row
  (Count selector) can clip; default 1250 px window is fine.
- Native scrollbars stay light in dark theme — consistent with the rest of
  the template's WinForms chrome; revisit only if desired.
- `System.Windows.Forms.DataVisualization` NuGet is now unused (see
  LatencyGraphControl above) — safe to drop from the csproj.
- App identity in `AppConfig` (AppName "Desktop App Template", About
  features "Test data 01–04", store links) still template placeholders.
