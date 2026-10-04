VDeskSwitcher

Tiny Windows tray app that switches virtual desktops when you rest the mouse cursor on the left or right edge of the screen, similar to screen edges in KDE Plasma.

How it works
Move the cursor to the right edge of the screen and hold it there -> switches to the next virtual desktop.
Move the cursor to the left edge and hold it there -> switches to the previous virtual desktop.
The delay before switching is configurable (default 500 ms).
After a switch, the trigger re-arms only once the cursor leaves the edge, so desktops don't cycle endlessly.
No switching while a mouse button is held (dragging windows, selecting text) or while a fullscreen app/game is in the foreground.
Edges are the outer edges of the whole virtual screen, so multi-monitor setups don't trigger on the border between two monitors.
Features
Runs quietly in the system tray, no taskbar window
Settings window: wait time before switching (100-5000 ms), applied instantly
Optional start with Windows (starts hidden in the tray)
Single instance only
Per-monitor DPI aware
Settings stored in %AppData%\DeskOverview\settings.json
Requirements
Windows 10 / 11 with virtual desktops
.NET 8+ Desktop Runtime (not needed for the self-contained build)
Usage
Run the exe. An icon appears in the system tray.
Double-click the icon (or use Settings in its menu) to change the delay.
Tray menu: Settings, Enable/Disable, Start with Windows, Exit.
Closing the settings window only hides it. Use Exit in the tray menu to quit.
Build
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
Notes

Windows has no official API for virtual desktops, so the app relies on a third-party wrapper over undocumented interfaces. If a major Windows update breaks it, the app falls back to simulating Win + Ctrl + Left/Right.

License

Add a license of your choice (e.g. MIT).
