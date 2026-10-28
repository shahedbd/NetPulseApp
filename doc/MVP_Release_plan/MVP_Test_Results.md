# NetPulse — MVP Release Test Results

**Build:** `dev` @ `cd28c8f` · app version 1.0.0 (`AppReleaseVersion` 1000), package manifest 1.0.2.0
**Tested:** 2026-10-28 · Windows 11 Pro 10.0.26200 · .NET SDK 11.0.100-rc.3 · target `net10.0-windows10.0.19041.0`, `win-x86`
**Network:** Wi-Fi, 2.4 GHz ch 11, 802.11n, WPA2-Personal, RSSI −37 dBm; ISP in BD

---

## 1. Verdict

**Not ready to submit yet.** Every tab's feature works, but one item blocks the release and two bugs should be fixed first:

| # | Severity | Area | Issue |
|---|---|---|---|
| B1 | **Blocker** | Packaging | `Package.appxmanifest` and `Package.StoreAssociation.xml` still have the **Battery ZX** identity: `Name="4789ZeroByte.BatteryZX"`, DisplayName "Battery ZX", a battery-app Description, and StartupTask `BatteryZxStartupTask`. Submitting this would ship as, or overwrite, Battery ZX. |
| B2 | High | Port Checker | An unresolvable host (e.g. `no-such-host.invalid`) marks every port **Filtered** in ~30 ms. No "Unknown host" error is shown. |
| B3 | High | WHOIS IP | **Every IPv6 lookup fails** with `400 (Bad Request)`. |
| B4 | Medium | Port Checker | Closed ports usually show as **Filtered** with the default 2 s timeout. |
| B5 | Decision | Scope | **F6 Background monitoring + alerts** is not built (P1 plan tasks 6.1–6.4 are still ☐). Either build it or move it to v1.1. |
| B6 | Review risk | Startup | On first run and on every update, the app opens the CPU ZX Pro Store page and a random tool URL by itself, and uploads device info. |

Details and fixes are in §4.

---

## 2. How it was tested

| Layer | Method |
|---|---|
| Build | `dotnet build` Debug (clean rebuild) and Release |
| Feature engines | A console test harness references `NetPulseApp.csproj` and drives each tab's service against the **live network**: real ICMP, DNS, TCP, HTTPS, RDAP, Cloudflare speed test and the Native Wi-Fi API. These are the same services the pages call. |
| UI | Each of the 11 nav pages is created in a hosted form at **1280 px and 760 px**. Each page runs for 1.5 s, switches dark ↔ light, is rendered to a bitmap, then disposed. The About dialog is opened and closed. |
| Code review | Release runtime switches, Store manifest, startup flow, error paths |

**Not covered (do these by hand before submitting):** see §5.

---

## 3. Results by tab

Totals: **96 checks: 89 pass, 4 fail, 3 warn.** All 4 failures are covered by B2 and B3. All 3 warnings are explained below as either by design or caused by the network.

### 3.1 Build

| Check | Result | Notes |
|---|---|---|
| Debug build | ✅ PASS | 0 errors |
| Release build (self-contained, trimmed partial) | ✅ PASS | 0 errors |
| Release runtime switches | ✅ PASS | `JsonSerializer.IsReflectionEnabledByDefault=true`, `BuiltInComInterop.IsSupported=true`, so settings JSON and WMI still work after trimming |
| Warnings | ⚠ INFO | 459 warnings, all nullable-reference (CS86xx) plus 1× CS0108. None block the release. |

### 3.2 Dashboard

| Check | Result | Notes |
|---|---|---|
| Interface monitor ticks (1 s) | ✅ PASS | 4 snapshots in 3.5 s |
| Active interface detected | ✅ PASS | Wi-Fi "WiFi", status Up |
| Local IP / Gateway / DNS | ✅ PASS | All filled in |
| Live throughput values | ✅ PASS | Non-negative; idle values ~0.03 / 0.07 Mbps |
| Public IP (provider chain) | ✅ PASS | IPv4 + country + ISP returned, 5.5 s on a cold run |
| VPN detector | ✅ PASS | 0 active, 8 installed but idle |
| UI load + theme + dispose (1280 / 760) | ✅ PASS | |

### 3.3 Diagnose

| Check | Result | Notes |
|---|---|---|
| Full run completes | ✅ PASS | 7 checks in 1.1 s |
| Adapter / Local IP / Gateway / DNS / Internet / HTTPS / Captive portal | ✅ PASS ×7 | Gateway 2 ms, DNS 79 ms (system) and 106 ms (1.1.1.1), Internet 28 ms, HTTPS 200 |
| Verdict | ✅ PASS | "Good: Your connection looks healthy" |
| Stop mid-run | ⚠ By design | No `RunFinished` after `Stop()`. The page has no Stop button and only stops when it is disposed, so nothing is affected. |
| UI (1280 / 760) | ✅ PASS | |

### 3.4 Speed Test

| Check | Result | Notes |
|---|---|---|
| Full test (Cloudflare) | ✅ PASS | 22 s total |
| Ping / jitter | ✅ PASS | 70 ms / 20.9 ms |
| Download / upload | ✅ PASS | 1.2 / 7.8 Mbps (slow test link) |
| Progress phases | ✅ PASS | Ping → Download → Upload → Complete |
| Cancel | ✅ PASS | Returned "Test cancelled." 2.5 s after cancel was requested |
| History store loads | ✅ PASS | 2 saved results |
| UI (1280 / 760) | ✅ PASS | |

### 3.5 Wi-Fi

| Check | Result | Notes |
|---|---|---|
| Current connection | ✅ PASS | RSSI −37, 95 %, 2.4 GHz ch 11, 20 MHz, Wi-Fi 4, WPA2-Personal, 144 Mbps |
| Channel math 2.4 / 5 / 6 GHz | ✅ PASS ×3 | 2437→6, 5180→36, 5955→1 |
| UI (1280 / 760) | ✅ PASS | |
| Location-denied / no-adapter states | ☐ Manual | Could not be tested on this machine (location allowed, adapter present) |

### 3.6 Ping

| Check | Result | Notes |
|---|---|---|
| 1.1.1.1 × 5 | ✅ PASS | 5/5, 28/36.6/47 ms, jitter 11 ms, 0 % loss |
| Quality grade | ✅ PASS | Grade C, plus a "Good for" line |
| Hostname (google.com) | ✅ PASS | Resolves, TTL shown |
| Unknown host | ✅ PASS | Status "Unknown host" |
| Unreachable IP | ✅ PASS | 0/2, Timeout |
| Continuous mode + Stop | ✅ PASS | 7 replies, stopped cleanly |
| UI (1280 / 760) | ✅ PASS | |

### 3.7 Traceroute

| Check | Result | Notes |
|---|---|---|
| Trace 1.1.1.1 | ✅ PASS | 8 hops, reaches the destination, 15–20 s |
| Hop numbering | ✅ PASS | Sequential, 1–8 |
| Reverse DNS | ✅ PASS (2nd run) | First run: no names within the 1.5 s rDNS timeout. Second run: `one.one.one.one`. The first-run miss came from this network's slow resolver, not from the app. |
| Unknown host | ✅ PASS | "Unknown host: no-such-host.invalid" |
| Stop mid-trace | ✅ PASS | |
| UI (1280 / 760) | ✅ PASS | |

### 3.8 DNS Lookup

| Check | Result | Notes |
|---|---|---|
| All types, system DNS | ✅ PASS | 25 records: A, AAAA, MX, TXT, NS, SOA |
| A via 1.1.1.1 | ✅ PASS | |
| MX via 8.8.8.8 | ✅ PASS | |
| TXT / AAAA | ✅ PASS | |
| NXDOMAIN | ✅ PASS | 0 rows; page shows "● No records" |
| Unreachable custom DNS server | ✅ PASS | One "ConnectionTimeout" error row after 6 s |
| PTR / reverse lookup | ⚠ Not in MVP | The page has no PTR type. Candidate for v1.1. |
| UI (1280 / 760) | ✅ PASS | |

### 3.9 Port Checker

| Check | Result | Notes |
|---|---|---|
| google.com 80 / 443 | ✅ PASS | Open (112 / 81 ms) |
| Non-listening ports | ✅ PASS | 22 and 8081 Filtered (dropped by firewall) |
| Closed port (127.0.0.1:1) | ❌ FAIL → **B4** | Shown as Filtered. Windows takes ~2.1 s to report a refusal (measured), which is longer than the timeout. |
| Unknown host | ❌ FAIL → **B2** | Every port Filtered in 33 ms and no error toast |
| 1024-port scan (localhost) | ✅ PASS | 1024 results in 0.7 s, 5 open |
| UI (1280 / 760) | ✅ PASS | |

### 3.10 WHOIS IP

| Check | Result | Notes |
|---|---|---|
| IPv4 8.8.8.8 | ✅ PASS | 9 fields (handle, name GOGL, range, …) |
| IPv6 2001:4860:4860::8888 | ❌ FAIL → **B3** | HTTP 400 |
| Domain google.com | ✅ PASS | 9 fields, registered 15 Sep 1997 |
| ccTLD example.co.uk | ✅ PASS | 11 fields |
| Unregistered domain | ✅ PASS | "No registration data found for …" |
| Bad input | ✅ PASS | Same "not found" message |
| UI (1280 / 760) | ✅ PASS | |

### 3.11 Subnet Calc

| Check | Result | Notes |
|---|---|---|
| 192.168.1.10/24 | ✅ PASS | Network .0, broadcast .255, 254 hosts, RFC 1918 |
| Dotted mask `10.0.0.5 255.255.255.0` | ✅ PASS | |
| Bare IP (= /32) | ✅ PASS | |
| /31 point-to-point | ✅ PASS | 2 usable hosts, no broadcast |
| /0 | ✅ PASS | |
| IPv6 2001:db8::1/64 | ✅ PASS | Labelled "Documentation" |
| /33, non-contiguous mask, garbage, empty | ✅ PASS ×4 | All rejected with a clear message |
| UI (1280 / 760) | ✅ PASS | |

### 3.12 Settings

| Check | Result | Notes |
|---|---|---|
| UI load + theme + dispose (1280 / 760) | ✅ PASS | Narrow layout path exercised at 760 px |
| Save / reset / search persistence | ☐ Manual | See §5 |

### 3.13 About

| Check | Result | Notes |
|---|---|---|
| Dialog opens and closes | ✅ PASS | |

---

## 4. Defects and fixes

### B1 — Store package has the Battery ZX identity (Blocker)
- `WinAppPakaging/Package.appxmanifest`: `Identity Name="4789ZeroByte.BatteryZX"`, `DisplayName`, `uap:VisualElements DisplayName/Description`, `ShortName`, and `StartupTask TaskId="BatteryZxStartupTask"` / `DisplayName`.
- `WinAppPakaging/Package.StoreAssociation.xml`: `MainPackageIdentityName` / `ReservedName` = Battery ZX.
- **Fix:** Reserve "NetPulse — Network Diagnostic Toolkit" in Partner Center. Then use *Project → Publish → Associate App with the Store* to regenerate both files. Set the Description from `doc/App Identity.md`. Also check that the `startupTask` extension is really needed, and rename its TaskId.
- Also: `MaxVersionTested="10.0.14393.0"` should be at least `10.0.19041.0` to match the TFM. In `NetPulseApp.csproj`, `Product` / `Description` / `PackageTags` still read "Desktop App Template".

### B2 — Port Checker reports an unknown host as Filtered
- `Service/Network/PortCheckService.cs` `ProbeAsync`: the catch-alls (`SocketException` → Filtered, bare `catch` → Filtered) swallow the DNS failure separately for every port. Because of that, the `RunError` path in `RunAsync`, which is meant for "unresolvable host, surfaces once", never runs.
- **Fix:** Resolve the host once in `RunAsync` before the probes, using `Dns.GetHostAddressesAsync`. On failure, raise `RunError("Unknown host: …")`. Then probe the resolved `IPAddress`. As a bonus, this stops 1024 parallel DNS lookups on a full scan.

### B3 — WHOIS fails for every IPv6 address
- `Service/Network/WhoisService.cs`: `Uri.EscapeDataString(target)` turns `:` into `%3A`, and rdap.org answers 400. Verified: the escaped URL returns 400, the raw `https://rdap.org/ip/2001:4860:4860::8888` returns 200.
- **Fix:** For IP targets, use the normalised `IPAddress.ToString()` without escaping. Keep the escaping for domains.

### B4 — Closed ports show as Filtered
- Windows retries the SYN after a RST, so a refused connect takes ~2 s (measured 2089 ms on loopback). The page's default timeout is 2 s (`PortCheckerControl.cs:116`, `SelectedIndex = 1`), and the 1 s option always loses.
- **Fix:** Make 3 s the default and drop the 1 s option. Or keep the options and explain in the page tooltip that "Filtered" can also mean "closed but slow to refuse".

### B5 — F6 Background monitoring not built
- Tasks 6.1–6.4 in `MVP_P1_Task_Plan.md` are ☐. `NetworkChangeMonitor` and `SystemTrayService` exist, but there is no connection monitor, drop alerts, outage log, or Settings toggles.
- **Decision needed:** Ship the MVP without F6 and list it as v1.1, or hold the release for about 1.5 days of work.

### B6 — Unrequested startup actions (Store review risk)
- `DeviceData/StartupService.cs` runs from `MainForm` on first install and on every version change. It opens `ms-windows-store://pdp/?productid=9N5RXJCZB734` (CPU ZX Pro) and a random tool URL with no user action, then posts device info to an API.
- **Risk:** Store policy 10.10 (promotion) and the privacy disclosure requirement (10.5.1). Make sure the listing has a privacy policy URL that covers the device-info upload. Consider opening these links only after the user clicks something.

---

## 5. Still to test by hand before submitting

1. **Run the shell itself** (`MainForm`): title bar, sidebar collapse, status bar Online / Local IP / Public IP, tray icon, single-instance message.
2. **Build and install the MSIX package** with `build-store-package.cmd`, then launch it from Start. This confirms the self-contained trimmed runtime starts on a machine without .NET 10.
3. **Run the Windows App Certification Kit** on the package.
4. **Wi-Fi with Location turned off** (Windows 11 24H2+): the page should show the Location-denied panel and its Settings link. Also test a PC with no Wi-Fi adapter (Ethernet only).
5. **Offline / airplane mode:** Dashboard, Diagnose (verdict should be "no internet"), Speed Test error text, WHOIS, Public IP from cache.
6. **Settings:** dark mode, sidebar, tray and startup toggles survive a restart; Reset; search.
7. **High DPI:** 150 % and 200 % scaling, plus moving the window between monitors with different DPI.
8. **Export / Copy** buttons on Ping, Traceroute, DNS, Port Checker, WHOIS and Subnet.

---

## 6. Re-test after fixes

After B1–B4 are fixed, re-run §3.9 (Port Checker) and §3.10 (WHOIS), and rebuild the package for §5.2–5.3. The rest of the test suite does not need to run again.
