# VDeskSwitcher

Tiny Windows tray app that lets you control virtual desktops with the mouse cursor at the screen edges, similar to screen edges in KDE Plasma:

- rest the cursor on the **left or right edge** to switch to the previous / next virtual desktop,
- rest it on the **top edge** to open Task View (same as pressing **Win + Tab**).

## How it works

- Move the cursor to the **right edge** of the screen and hold it there -> switches to the **next** virtual desktop.
- Move the cursor to the **left edge** and hold it there -> switches to the **previous** virtual desktop.
- Move the cursor to the **top edge** and hold it there -> opens **Task View** (Win + Tab) with all your virtual desktops and windows.
- The delay before an action triggers is configurable (default 500 ms).
- After an action, the trigger re-arms only once the cursor leaves the edge, so desktops don't cycle endlessly and Task View doesn't reopen repeatedly.
- No action while a mouse button is held (dragging windows, selecting text) or while a fullscreen app/game is in the foreground.
- Edges are the outer edges of the whole virtual screen, so multi-monitor setups don't trigger on the border between two monitors.

## Features

- Left / right edge: switch to the previous / next virtual desktop
- Top edge: open Task View (Win + Tab)
- Runs quietly in the system tray, no taskbar window
- Settings window: wait time before triggering (100-5000 ms), applied instantly
- Optional start with Windows (starts hidden in the tray)
- Single instance only
- Per-monitor DPI aware
- Settings stored in `%AppData%\DeskOverview\settings.json`

## Requirements

- Windows 10 / 11 with virtual desktops
- .NET 10 Desktop Runtime (not needed for the self-contained build)

## Usage

1. Run the exe. An icon appears in the system tray.
2. Double-click the icon (or use **Settings** in its menu) to change the delay.
3. Tray menu: **Settings**, **Enable/Disable**, **Start with Windows**, **Exit**.
4. Closing the settings window only hides it. Use **Exit** in the tray menu to quit.

## Build

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

## Notes

Windows has no official API for virtual desktops, so the app relies on a third-party wrapper over undocumented interfaces. If a major Windows update breaks it, the app falls back to simulating `Win + Ctrl + Left/Right`.

The exe is not code-signed, so Windows SmartScreen may show a warning on first run (More info -> Run anyway).

## License

Add a license of your choice (e.g. MIT).
