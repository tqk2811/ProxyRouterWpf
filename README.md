# ProxyRouter WPF

A Windows desktop (WPF) port of [ProxyRouter](https://github.com/tqk2811/ProxyRouter) — a local
multi-protocol proxy server that listens on local ports and forwards traffic to upstream proxies,
with host-based routing rules. This desktop edition drops the ASP.NET web stack and the SQL database.

## What it does

- Runs local **HTTP / SOCKS4 / SOCKS5** listeners (protocol auto-detected per connection) on
  `StartPort + i`, one listener per ungrouped upstream proxy source.
- **Group + Filter routing**: override the upstream per request by target host — `Wildcard`,
  `Equals`, `StartsWith`, `EndsWith`, `Contains`, `CIDR`, `Regex`, or cumulative `TotalBytes`
  thresholds, combined with `And`/`Or` match modes and `NOT` negation.
- **Tunnel logs** kept in a bounded in-memory FIFO (oldest dropped first) with filtering, sorting,
  paging and a read-only detail view.
- **Bandwidth** monitor: whole-machine realtime chart (WMI network counters).
- **Dark / Light / System** theme (live switch), styled after
  [AndroidSyncControl](https://github.com/tqk2811/AndroidSyncControl).

## Screenshots

### Proxies tab — listeners, routing groups and filters

| Light | Dark |
| --- | --- |
| ![Proxies tab, light theme](docs/images/proxies-light.png) | ![Proxies tab, dark theme](docs/images/proxies-dark.png) |

### Logs tab — filtering, paging and the RAM FIFO limit

![Tunnel logs](docs/images/logs.png)

### Bandwidth tab — realtime upload / download chart

![Bandwidth monitor](docs/images/bandwidth.png)

## Differences from the original (by design)

- **No database.** Proxy configuration is stored in a JSON file (`proxyrouter.config.json`) next to
  the executable; tunnel logs live only in RAM.
- **No login / users** — single-user desktop app.
- Removed pages: `Dashboard` home, `Dashboard/IpWhiteList`, `Dashboard/Admin/Log`.
- **No auto-start**: the proxy engine never starts on launch. Enable it from the **Proxies** tab.

## Requirements

- Windows 10/11
- .NET 8 SDK (`net8.0-windows`)

## Build & run

```bash
dotnet build ProxyRouterWpf.slnx -c Release
dotnet run --project src/ProxyRouterWpf/ProxyRouterWpf.csproj
```

## Versioning & releases

Versions come from [GitVersion](GitVersion.yml): a tag `vM.N.0` opens a minor line and every
build on it is `M.N.<commits-since-tag>` (`1.0.0`, `1.0.1`, …). Debug builds skip GitVersion and
report `0.0.0-debug`. Bump a line by tagging `vM.<N+1>.0` and pushing the tag.

Releases are **master-only** and opt-in: [`.github/workflows/release.yml`](.github/workflows/release.yml)
runs only when the pushed head commit contains the marker `[release]` (or on a manual dispatch
from master). It publishes a framework-dependent `win-x64` build, zips it to
`ProxyRouterWpf-M.N.<n>-win-x64.zip`, and attaches it to the GitHub Release named after the tag
`vM.N.0`, whose notes list every `[release]` build since that tag.

```powershell
.\Changelog.ps1                        # regenerate CHANGELOG.md (git-cliff, Conventional Commits)
.\Release.ps1 -Message "msg" -Push     # changelog + commit with [release] + push (triggers CI)
```

Commits must follow [Conventional Commits](https://www.conventionalcommits.org) — the changelog
and the release notes are generated from them. To rebuild only the notes of an existing Release,
push a commit marked `[release_notes_only]` or dispatch the workflow with that input.

## Project layout

```
src/ProxyRouterWpf/
  Enums/            domain enums (proxy type, filter type, outcomes, ...)
  Models/           config + view models (POCOs, JSON-persisted)
  Configuration/    ConfigStore (JSON), AppServices (composition root)
  Services/         single-user in-memory CRUD (sources / groups / filters / configure)
  Proxy/            proxy engine (manager, session, handlers) over TqkLibrary.Proxy
    EventLogs/      RAM tunnel-log pipeline (FIFO store, channel consumer, traffic cache)
  Bandwidth/        WMI sampler + ring-buffer cache
  Themes/           Colors.Dark/Light + Controls.xaml + ThemeManager
  Converters/       value converters
  ViewModels/       MVVM (CommunityToolkit.Mvvm)
  Views/            tabs (Proxies, Logs, Bandwidth, Settings) + dialogs
```

## Credits

Core proxy engine: [`TqkLibrary.Proxy`](https://www.nuget.org/packages/TqkLibrary.Proxy).
