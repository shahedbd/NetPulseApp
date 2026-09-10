# NetPulse Toolkit — UI Wireframe (ASCII)

**Product:** NetPulse Toolkit — all-in-one ping / DNS / port / WHOIS network diagnostics app
**Doc type:** Low-fidelity UI wireframe (ASCII), pre-visual-design
**Status:** Draft for review
**Reference:** `Microsoft_Store_Next_10_App_Ideas_Research.md` (#1 — NetPulse Toolkit, opportunity score 71.8/100)
**Platform:** Windows desktop, WinForms (consistent with Zero Byte's existing stack)
**Date:** 2026-09-08

## Overview

NetPulse Toolkit answers "why is my network broken?" in one window instead of five `cmd.exe` commands. The wireframes below cover the full app shell plus every core tool named in the product brief — Ping, DNS Lookup, Port Checker, and WHOIS/Public IP — along with the killer feature (one-click **Diagnose My Connection**) and the remaining toolkit screens (Traceroute, Subnet Calculator, Settings) shown in compact form for completeness.

These are structural wireframes, not visual design: they define layout, hierarchy, and the controls each screen needs, not colors, fonts, or spacing.

## Wireframe Legend

- `[ Label ]` — a button
- `[x]` / `[ ]` — a checked / unchecked checkbox
- `( ● )` / `( ○ )` — a selected / unselected radio option
- `[ value ▼ ]` — a dropdown field
- `[==>---]` — a progress bar
- `>` inside a button — a "run/start" action (e.g. `[ > Start ]`)
- `nav = X` — which left-nav item is active while this screen is showing
- Box-drawn rectangles (`┌─┐│└┘`) — panels, cards, and tables; nesting shows visual grouping

## App Shell

Every screen below renders inside this frame. The left nav rail is always visible so the user can jump between tools mid-diagnosis; the status bar keeps local/public IP visible at all times, which doubles as a live sanity check that the network is up.

### APP SHELL — Global Frame (title bar / left nav / content / status bar)

```
┌────────────────────────────────────────────────────────────────────────────┐
│NetPulse Toolkit                                                  —   ▢   × │
├────────────────────────────────────────────────────────────────────────────┤
│                    │                                                       │
│ > DASHBOARD        │   (active screen renders here)                        │
│   PING             │                                                       │
│   TRACEROUTE       │                                                       │
│   DNS LOOKUP       │                                                       │
│   PORT CHECKER     │                                                       │
│   WHOIS / IP       │                                                       │
│   SUBNET CALC      │                                                       │
│ ------------------ │                                                       │
│   SETTINGS         │                                                       │
│   UPGRADE TO PRO   │                                                       │
│                    │                                                       │
├────────────────────────────────────────────────────────────────────────────┤
│ ● Online   Local IP: 192.168.1.24   Public IP: 203.0.113.7        v1.0.0   │
└────────────────────────────────────────────────────────────────────────────┘
```

## Dashboard (Diagnose My Connection)

This is the landing screen and the app's differentiator: a plain-English "what's wrong" answer instead of making the user pick a tool first. It has three states.

### DASHBOARD — Idle state (content pane, nav = DASHBOARD)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Dashboard                                                                  │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │                                                                  │      │
│  │        Something not working? Let's find out.                    │      │
│  │                                                                  │      │
│  │          [   >  DIAGNOSE MY CONNECTION   ]                       │      │
│  │                                                                  │      │
│  │   Runs ping + DNS + gateway checks in about 10 seconds           │      │
│  │                                                                  │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                                                                            │
│ Quick Tools                                                                │
│                                                                            │
│ [ Ping ]   [ Traceroute ]   [ DNS Lookup ]                                 │
│ [ Port Checker ]   [ WHOIS / IP ]   [ Subnet Calc ]                        │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│ Last diagnosis: 2 hours ago — All systems normal      [ View history ]     │
└────────────────────────────────────────────────────────────────────────────┘
```

### DASHBOARD — Running state (modal progress overlay)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Dashboard   (dimmed background)                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│            ┌──────────────────────────────────────────────────┐            │
│            │                                                  │            │
│            │        Diagnosing your connection...             │            │
│            │                                                  │            │
│            │   [====================>-----------]  62%        │            │
│            │                                                  │            │
│            │   [x] Checking local network adapter             │            │
│            │   [x] Pinging default gateway                    │            │
│            │   [x] Resolving DNS (8.8.8.8)                    │            │
│            │   [ ] Pinging google.com                         │            │
│            │   [ ] Checking public IP reachability            │            │
│            │                                                  │            │
│            │                [ Cancel ]                        │            │
│            │                                                  │            │
│            └──────────────────────────────────────────────────┘            │
│                                                                            │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

### DASHBOARD — Results state (after Diagnose My Connection completes)

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Dashboard — Diagnosis Complete                                             │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │                                                                  │      │
│  │   RESULT: Your connection to the internet is fine, but           │      │
│  │   your DNS server is responding slowly (380 ms avg).             │      │
│  │                                                                  │      │
│  │   Suggested fix: switch to a public DNS (1.1.1.1 or              │      │
│  │   8.8.8.8) in Settings > Network.                                │      │
│  │                                                                  │      │
│  │   [ Apply Suggested DNS ]     [ Dismiss ]                        │      │
│  │                                                                  │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                                                                            │
│ Details                                                                    │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │ CHECK                    STATUS     DETAIL                       │      │
│  ├──────────────────────────────────────────────────────────────────┤      │
│  │ Network adapter          [ OK ]     Connected (Wi-Fi)            │      │
│  │ Default gateway          [ OK ]     1.2 ms                       │      │
│  │ DNS resolution           [SLOW]     380 ms avg (8.8.8.8)         │      │
│  │ Ping google.com          [ OK ]     14 ms avg, 0% loss           │      │
│  │ Public IP reachable      [ OK ]     203.0.113.7                  │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│        [ Export Report ]        [ Run Again ]                              │
└────────────────────────────────────────────────────────────────────────────┘
```

## Ping

Live latency graph plus a scrolling reply log, matching the brief's "ping with live graph" feature. Continuous mode is the default so the graph always has something to show.

### PING — Target input, live graph, results table

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Ping                                            nav = PING                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Target  [ google.com                    ▼ ]   Interval [ 1s ▼ ]            │
│                                                                            │
│ [ > Start ]   [ ▢ Stop ]   [ Continuous ●  Count: 4 ○ ]                    │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ Response Time (ms)                                    Live           │  │
│  │ 60 |                                                                 │  │
│  │ 45 |        _                                 _                      │  │
│  │ 30 |    _  / \    _      _              _    / \  _                  │  │
│  │ 15 |___/ \/   \__/ \____/ \____________/ \___/   \/ \___             │  │
│  │  0 +----------------------------------------------------             │  │
│  │     00:00        00:10        00:20        00:30   now               │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ SEQ    TIME        TTL    STATUS                                     │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │ 41     13 ms       118    Reply                                      │  │
│  │ 42     14 ms       118    Reply                                      │  │
│  │ 43     —           —      Timeout                                    │  │
│  │ 44     12 ms       118    Reply                                      │  │
│  │ 45     15 ms       118    Reply                                      │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│ Sent: 45   Received: 44   Lost: 1 (2.2%)   Min/Avg/Max: 12/13.6/15 ms      │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                    [ Export Report ]      [ Copy Results ]                 │
└────────────────────────────────────────────────────────────────────────────┘
```

## DNS Lookup

Covers A/AAAA/MX/TXT/CNAME/NS as called out in the core features list, with an "All" option so a user can dump every record type for a domain in one query.

### DNS LOOKUP — domain input, record-type filter, results table

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ DNS Lookup                                nav = DNS LOOKUP                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Domain  [ example.com                        ]   [ > Lookup ]              │
│                                                                            │
│ Record type  ( A )( AAAA )( MX )( TXT )( CNAME )( NS )( All ●)             │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ TYPE     NAME               VALUE                        TTL         │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │ A        example.com        93.184.216.34                 300s       │  │
│  │ AAAA     example.com        2606:2800:220:1:248:1893::    300s       │  │
│  │ MX       example.com        10 mail.example.com            3600s     │  │
│  │ TXT      example.com        "v=spf1 -all"                 3600s      │  │
│  │ NS       example.com        ns1.example.com                86400s    │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
│ Resolved via: system default resolver (8.8.8.8)   Query time: 42 ms        │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                    [ Export Report ]      [ Copy Results ]                 │
└────────────────────────────────────────────────────────────────────────────┘
```

## Port Checker

TCP (and optionally UDP) port checks against a host, single port or comma/range list, with a "Common Ports" preset so users don't need to know port numbers by heart.

### PORT CHECKER — host/port input, protocol, scan progress, results table

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Port Checker                            nav = PORT CHECKER                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Host/IP [ example.com                ]  Port(s) [ 21,22,80,443,3389 ]      │
│                                                                            │
│ Protocol ( TCP ●)( UDP ○)     [ Common Ports ▼ ]     [ > Scan ]            │
│                                                                            │
│ Scanning...  [==================>---------]  68%   (17 of 25 ports)        │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ PORT   STATUS       SERVICE (GUESS)      RESPONSE                    │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │ 21     Closed       FTP                  —                           │  │
│  │ 22     Open         SSH                  9 ms                        │  │
│  │ 80     Open         HTTP                 11 ms                       │  │
│  │ 443    Open         HTTPS                10 ms                       │  │
│  │ 3389   Filtered     RDP                  timeout                     │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                    [ Export Report ]      [ Copy Results ]                 │
└────────────────────────────────────────────────────────────────────────────┘
```

## WHOIS / Public IP

Two cards side by side: the user's own public IP/ISP/geolocation (auto-populated on screen load) and a WHOIS lookup for any domain or IP they type in — the two most-requested "what is my..." and "who owns..." questions in one place.

### WHOIS / PUBLIC IP — your-IP card + WHOIS lookup card, side by side

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ WHOIS / IP                                nav = WHOIS / IP                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ ┌────────────────────────────────┐   ┌──────────────────────────────────┐  │
│ │                                │   │                                  │  │
│ │  YOUR PUBLIC IP                │   │  WHOIS RESULT: example.com       │  │
│ │                                │   │                                  │  │
│ │  203.0.113.7                   │   │  Registrar:    Example Registrar │  │
│ │                                │   │  Created:      1997-08-15        │  │
│ │  ISP:      Example Telecom     │   │  Expires:      2027-08-14        │  │
│ │  City:     Dhaka, BD           │   │  Name servers: ns1.example.com   │  │
│ │  ASN:      AS12345             │   │                ns2.example.com   │  │
│ │                                │   │  Org:          Example Corp.     │  │
│ │  [ Copy ]   [ Refresh ]        │   │                                  │  │
│ │                                │   └──────────────────────────────────┘  │
│ └────────────────────────────────┘                                         │
│                                                                            │
│ Lookup domain or IP  [ example.com                  ]   [ > Lookup ]       │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                    [ Export Report ]      [ Copy Results ]                 │
└────────────────────────────────────────────────────────────────────────────┘
```

## Secondary Screens (Compact)

The remaining core-feature-list tools — Traceroute and Subnet/CIDR Calculator — plus Settings, shown at lower fidelity since they follow the same input-then-results pattern as the screens above.

### TRACEROUTE (compact) — secondary screen

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Traceroute                              nav = TRACEROUTE                   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Target [ google.com                     ]   Max hops [ 30 ▼ ]  [ > Start ] │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────────┐  │
│  │ HOP   HOSTNAME                    IP              RTT 1/2/3          │  │
│  ├──────────────────────────────────────────────────────────────────────┤  │
│  │ 1     router.home                 192.168.1.1     1/1/1 ms           │  │
│  │ 2     10.10.0.1                   10.10.0.1       8/7/9 ms           │  │
│  │ 3     isp-core-1.example.net      203.0.113.1     14/13/15 ms        │  │
│  │ 4     *                            *               timeout           │  │
│  │ 5     google-peer.net             142.250.4.1     22/21/23 ms        │  │
│  └──────────────────────────────────────────────────────────────────────┘  │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                    [ Export Report ]      [ Copy Results ]                 │
└────────────────────────────────────────────────────────────────────────────┘
```

### SUBNET CALCULATOR (compact) — secondary screen

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Subnet Calculator                       nav = SUBNET CALC                  │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ CIDR / IP  [ 192.168.1.0/24                          ]  [ > Calculate ]    │
│                                                                            │
│  ┌──────────────────────────────────────────────────────────────────┐      │
│  │ Network Address:    192.168.1.0                                  │      │
│  │ Broadcast Address:  192.168.1.255                                │      │
│  │ Usable Host Range:  192.168.1.1 - 192.168.1.254                  │      │
│  │ Subnet Mask:        255.255.255.0                                │      │
│  │ Total Hosts:        254                                          │      │
│  └──────────────────────────────────────────────────────────────────┘      │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│                            [ Copy Results ]                                │
└────────────────────────────────────────────────────────────────────────────┘
```

### SETTINGS (compact) — secondary screen

```
┌────────────────────────────────────────────────────────────────────────────┐
│                                                                            │
│ Settings                                    nav = SETTINGS                 │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│ Default ping target       [ google.com                    ]                │
│ Preferred DNS resolver    [ System default             ▼ ]                 │
│ Units                     ( ms ●)( s ○ )                                   │
│ Start with Windows        [x]                                              │
│ Theme                     ( Light ○ )( Dark ●)( System ○ )                 │
│                                                                            │
├────────────────────────────────────────────────────────────────────────────┤
│ NetPulse Toolkit Pro                                                       │
│ Unlock: scheduled monitoring, outage alerts, remote port                   │
│ check relay.                              [ Upgrade — $4.99 ]              │
└────────────────────────────────────────────────────────────────────────────┘
```

## Navigation Map

### NAVIGATION MAP — how screens relate

```
                              +---------------+
                              |   DASHBOARD   |
                              | (Diagnose My  |
                              |  Connection)  |
                              +-------+-------+
                                      |
        +--------------+-------------+-------------+--------------+
        |              |             |              |              |
        v              v             v              v              v
  +----------+   +-----------+  +----------+  +-----------+  +-----------+
  |   PING   |   |TRACEROUTE |  |   DNS    |  |   PORT    |  |  WHOIS /  |
  |          |   |           |  | LOOKUP   |  | CHECKER   |  |    IP     |
  +----------+   +-----------+  +----------+  +-----------+  +-----------+
        |              |             |              |              |
        +--------------+------+------+--------------+--------------+
                              |
                              v
                       +-------------+
                       |  SUBNET     |
                       |  CALCULATOR |
                       +-------------+

  All tool screens share: [ Export Report ] [ Copy Results ] actions,
  and are reachable from the left nav rail at all times (see App Shell).
```

## Notes & Open Questions

The free/Pro split follows the CPU ZX / CPU ZX Pro precedent noted in the research doc: core toolkit free, with scheduled monitoring, outage alerts, and the remote port-check relay gated behind the Settings screen's upgrade card — that gate is sketched here but not yet placed on individual tool screens (e.g., whether "Scheduled connectivity monitoring" gets its own nav item once built). The results-table pattern (Ping, DNS, Port Checker, Traceroute) is intentionally identical across tools so it can become one shared WinForms control rather than four separate implementations. Icon assets for the nav rail are left as plain labels here; actual glyphs should follow whatever icon set the existing Zero Byte apps (Net Speed Meter, CPU ZX Pro) already use, for visual consistency across the portfolio.
