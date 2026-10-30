# Taskbar Status — Brief Plan

**Stack:** independent C# app · .NET 10 · WinUI 3 taskbar UI · **WinForms Settings window and tray icon**.
**Candidate target:** Windows 11 24H2+ / Deskband11Lib.WinUI 1.4.2. No Windhawk or C++ project.

## Taskbar layout

Keep the original two-row hardware frame on the left. Only the right frame changes; frames are containers in one window, not separate windows.

```text
┌─ Hardware frame ────────────┬─ Selected module ──────────────┐
│ CPU  10%  55°C  [graph]     │ Module row 1                  │
│ GPU   6%  48°C  [graph]     │ Module row 2                  │
└────────────────────────────┴───────────────────────────────┘
```

Choose the module in the **WinForms Settings window**. RAM+VRAM is the default. Hardware remains present while space permits; compact/hide rules apply when the taskbar fills.

### Right-frame module layouts

**RAM + VRAM**
```text
┌──────────────────────────────┐
│ RAM    42%    13.4 / 32 GB    │
│ VRAM   10%     1.6 / 16 GB    │
└──────────────────────────────┘
```

**SMTC / Now playing**
```text
┌────────┬─────────────────────┐
│ Cover  │ Track title →       │
│ art    │  [⏮]   [⏯]   [⏭]   │
└────────┴─────────────────────┘
```
Scroll long titles; use a placeholder without cover art, show “No media” without a session, and disable unsupported buttons.

**Live lava lamp — Win2D**
```text
┌──────────────────────────────┐
│    ●    ◒     ●              │
│      ◕      ●    ◔           │
└──────────────────────────────┘
```
Procedurally draw moving, merging blobs. Suggested configurable mappings: CPU usage → speed/turbulence; CPU/GPU temperature → color; RAM usage → blob size or occupied area.

- Render with `Microsoft.Graphics.Win2D` / `CanvasAnimatedControl` inside the module.
- Reuse hardware snapshots (about 1-second sampling); animate smoothly at an initial 30 FPS target.
- Pass an immutable readings/settings snapshot to the animation loop.
- Pause animation when hidden, switched away, or monitoring is paused; release graphics resources on window recreation/exit.
- Use a neutral color/idle movement when a selected sensor is unavailable.
- Start with a few blobs and simplified merging effects; profile resource consumption before increasing visual complexity.

[Win2D C#/WinUI animation reference](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/quick-start).

**Custom image/GIF holder**
```text
┌──────────────────────────────┐
│      Image / animated GIF    │
│       Fit or Fill area       │
└──────────────────────────────┘
```
Allow custom GIF, PNG, JPG/JPEG, Fit/Fill, and an empty-state placeholder. Validate GIF decoding/playback during the prototype.

**Small game:** optional and postponed until the very end; no implementation in the initial scope.

## WinForms Settings window

```text
Module:        [ RAM+VRAM / SMTC / Live lava lamp / Image-GIF ▼ ]
Show usage:    [✓] CPU usage  [✓] GPU usage
Show temp:     [✓] CPU temp   [✓] GPU temp   [✓] Mini graphs
Colors:        [Text] [Graph] [Warning]
Warnings:      CPU [85]°C    GPU [85]°C
Refresh:       [1000] ms     [ ] Start with Windows
Lava lamp:     [Motion source] [Temperature source] [Colors]
               [RAM-size mapping] [Intensity] [FPS]
Image/GIF:     [Choose file] [Fit / Fill] [Clear]
               [Save] [Cancel]
```

- Show/hide CPU/GPU usage and temperature independently; configure each temperature warning threshold.
- Save applies changes without restart; Cancel discards edits. Persist lava-lamp data mappings, palette, intensity, and frame-rate target with the other preferences.
- Tray left-click opens this form; right-click offers Settings, Pause monitoring, Exit.
- Store preferences in `%LocalAppData%\TaskbarStatus\settings.json`; copy imported images to `%LocalAppData%\TaskbarStatus\Media\`.
- Host WinForms settings on an STA message-loop thread; send taskbar changes to the WinUI `DispatcherQueue`. Exit stops both UI lifetimes.

## File structure

```text
TaskbarStatus/
├─ TaskbarStatus.sln                        # Solution
├─ README.md                                # Build and usage
│
├─ .github/                                 # GitHub automation
│  └─ workflows/                            # Build and release workflows
│     └─ release.yml                        # Build, package, attest, release
│
├─ installer/                               # Installer packaging
│  └─ TaskbarStatus.iss                     # Inno Setup installer
│
└─ src/                                     # Application source
   └─ TaskbarStatus/                        # One C# project
      ├─ TaskbarStatus.csproj               # WinUI, WinForms, LibreHardwareMonitor, Deskband11Lib.WinUI, Win2D
      ├─ App.xaml                           # Shared UI resources
      ├─ App.xaml.cs                        # Startup; owns tray, services, TaskbarHost, settings, exit
      │
      ├─ Services/                          # Readings, media commands, saved preferences
      │  ├─ HardwareMonitorService.cs       # Sensor polling and readings record
      │  ├─ MediaSessionService.cs          # SMTC session, cover art, button commands
      │  └─ SettingsService.cs              # Settings variables, JSON, startup option, image paths
      │
      ├─ UI/                                # Two UI surfaces
      │  ├─ TaskbarWindow.xaml              # RootGrid: hardware + RAM/SMTC/lava-lamp/image views
      │  ├─ TaskbarWindow.xaml.cs           # Bind readings/settings, switch modules, media clicks, renderer lifetime
      │  ├─ LavaLampRenderer.cs             # Win2D blobs, movement, data-driven colors, graphics cleanup
      │  └─ SettingsForm.cs                 # WinForms controls, lava-lamp mappings, Save/Cancel
      │
      ├─ Platform/                          # Library adapter, not a second taskbar implementation
      │  └─ TaskbarHost.cs                  # Deskband11Lib: attach, track size, recreate, dispose
      │
      └─ Assets/                            # Built-in resources
         └─ tray.ico                        # Tray icon
```

**8 C# files + 2 XAML files.** WinUI generates `Main()`; our startup is `App.OnLaunched` in `App.xaml.cs`.

## Taskbar library: what we use and where

[Deskband11Lib.WinUI 1.4.2](https://www.nuget.org/packages/Deskband11Lib.WinUI/1.4.2): C#/.NET NuGet package by `airtaxi`, MIT license; Core comes transitively. Checked package target: .NET 10 / Windows build 26100. Independent audit not found; signatures/provenance still unverified.

1. Reference the pinned package in `TaskbarStatus.csproj`.
2. `App.xaml.cs` creates `TaskbarWindow` and our `Platform/TaskbarHost.cs` adapter.
3. The adapter creates and retains the library's `TaskbarContentHost` using the window and its named `RootGrid` XAML container:

```csharp
using Deskband11Lib.Core;
using Deskband11Lib.WinUI;

// In TaskbarHost; root is the RootGrid passed from TaskbarWindow.
_host = new TaskbarContentHost(window, root, new TaskbarContentHostOptions
{
    PreferredWidth = 360,
    PreferredHeight = 48,
    AllowFixedSlotResize = true,
    TrackTaskbarButtons = true,
    TrackNotificationArea = true
});
await _host.AttachWhenLayoutReadyAsync();
window.Activate();
```

4. The library parents our window to the taskbar and measures free space through UI Automation; it does not push app icons aside.
5. Our window responds to width changes with Full / Compact / Hidden layouts. The library handles host sizing, not which metrics to hide.
6. Before attaching, subscribe to `TaskbarWindowRecreationRequired`: recreate the window/host after Explorer/DPI/monitor changes. Dispose the old host and unsubscribe events on replacement or exit.

[Library usage and lifecycle](https://github.com/airtaxi/Deskband11Lib#basic-usage). This is a candidate integration to validate, not an already-tested implementation.

## APIs

| Need | API / library |
|---|---|
| CPU/GPU temperature, usage; VRAM usage/capacity | `LibreHardwareMonitorLib` |
| RAM usage/capacity | `GlobalMemoryStatusEx` |
| Current media session, metadata, controls | `Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager` |
| Taskbar attachment / available-space tracking | `Deskband11Lib.WinUI.TaskbarContentHost` |
| Live lava-lamp graphics / animation | `Microsoft.Graphics.Win2D` / `CanvasAnimatedControl` |
| Tray / JSON | WinForms `NotifyIcon`, `ContextMenuStrip` / `System.Text.Json` |

SMTC consumes other apps' sessions: get metadata with `TryGetMediaPropertiesAsync()`; buttons call `TrySkipPreviousAsync()`, `TryTogglePlayPauseAsync()`, and `TrySkipNextAsync()`. Listen for session/track/playback changes, check supported controls/results, and marshal UI updates. [SMTC API reference](https://learn.microsoft.com/en-us/uwp/api/windows.media.control)

## Phases

- [ ] Build startup, WinForms settings/tray, hardware readings, and JSON persistence.
- [ ] Prototype the hardware frame plus RAM+VRAM, SMTC, and custom image/GIF modules in a normal WinUI window.
- [ ] Prototype the live lava lamp: moving/merging blobs → temperature colors → usage-driven motion and RAM-size mapping; test configurable mappings, target FPS, hidden-state pausing, and resource consumption.
- [ ] **Later investigation:** inspect the library adapter/parenting/sizing; test app-count changes, left/center alignment, responsive states, DPI, multiple monitors, auto-hide/fullscreen, clicks, Explorer restart, Win2D drawing/resizing/device recovery, overhead, cleanup, and package provenance.
- [ ] Finalize taskbar integration only after that investigation.
- [ ] Publish an Inno Setup installer and portable ZIP; validate .NET/WinUI runtime bundling.
- [ ] GitHub workflow: build PRs/`main`; `v*` tags package, SHA-256 checksum, attest both final files using [`actions/attest`](https://github.com/actions/attest), and publish a release. Write/OIDC permissions are release-only.
- [ ] Reconsider the game last.
