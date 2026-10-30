# DriversGet

A tiny Windows system-tray app (C# 5 / .NET Framework 4.8, WinForms) that helps you
get the latest **official** hardware drivers. Styled after the TrimKit dark theme.

It mirrors the "Driver Tools" tab from DriverUpdate-Pro: a 2x2 grid of the four
main driver sources.

## Features

- **NVIDIA** - looks up and **downloads the latest official Game Ready (DCH) driver
  directly** via NVIDIA's public driver lookup service, no browser required.
- **AMD** - opens AMD's official Auto-Detect / Adrenalin download tool.
- **Intel** - opens the Intel Driver & Support Assistant.
- **Windows Update** - opens the Windows Update settings page (chipset, audio,
  printer drivers etc.).
- **Official Links** menu - direct links to every vendor's official site and
  detection tool.
- **System tray** - minimizes to the tray; right-click for Open / Check Now /
  Settings / Exit.
- **Periodic check** - checks NVIDIA's latest version on an interval and shows a
  tray balloon when a newer driver is available.
- **Settings** - download folder, check interval, check-on-startup, start-minimized.
  Stored in `%APPDATA%\DriversGet\settings.ini`.

## Why only NVIDIA auto-downloads

NVIDIA exposes a stable public lookup service that returns a direct installer URL.
AMD (Cloudflare-protected), Intel, and Realtek do not offer a stable public
download API, so for those the app opens the official vendor tool/page instead of
scraping fragile URLs. For Realtek and other component-level drivers, use your
laptop or motherboard manufacturer's support site.

## Build

No Visual Studio, MSBuild, or dotnet SDK required - the app builds with the
in-box .NET Framework compiler (`csc.exe`).

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1
```

Output: `bin\DriversGet.exe` (plus `DriversGet.exe.config`).

## Run

```powershell
.\bin\DriversGet.exe
```

The window opens by default (set "Start minimized to tray" in Settings to launch
hidden). Closing the window hides it to the tray; use the tray icon's **Exit** to
quit fully.

## Project layout

```
src/
  Program.cs          App entry, single-instance guard, tray icon, periodic timer
  MainForm.cs         Main window: 2x2 supplier card grid, progress, log
  SettingsForm.cs     Settings dialog
  DriverSources.cs    Vendor links + NVIDIA driver lookup API
  Downloader.cs       Direct HTTP download with progress (no browser needed)
  AppSettings.cs      INI-style settings persistence
  SystemInfo.cs       WMI GPU/OS detection
  Theme.cs            TrimKit-style dark palette + styled controls
  CardPanel.cs        Rounded card panel
  App.config          Runtime config (TLS 1.2)
build.ps1             csc.exe build script
```
