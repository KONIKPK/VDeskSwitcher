# DeskOverview edge switcher

A tray edge switcher with one settings window. The solution/project and executable remain named `VDeskSwitcher`.

## Behavior

- Hold the cursor at the outer right/left edge of the entire virtual screen to switch next/previous desktop. Default dwell: 500 ms.
- Hold at the primary monitor's top edge for the same dwell to open Windows Task View (Win+Tab). Other monitors' top edges do not trigger it; side switching takes priority at shared corners.
- Move at least 20 physical pixels inward before returning to trigger again.
- Move at least 20 px down from the primary top edge before triggering Task View again. Remaining at the edge does not repeatedly send Win+Tab.
- No edge actions while any mouse button is held or a fullscreen foreground window covers its monitor.
- No wrapping by default. Settings and short comments are in `EdgeSettings.cs`.
- Manual/F5 startup opens settings; `VDeskSwitcher.exe --tray` starts hidden. Settings never appear on the taskbar.
- Closing or minimizing settings hides the window; double-click the icon or choose `Nastavenia` to reopen it.
- The integer slider accepts 100–5000 ms and applies/saves immediately to `%AppData%\DeskOverview\settings.json`. Missing/damaged files use defaults; out-of-range values are clamped.
- Tray menu: `Nastavenia`, `Zapnúť/Vypnúť`, `Spustiť pri štarte Windows` (checked when enabled), `Ukončiť`.
- Autostart uses `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\VDeskSwitcher` with `"<path>\VDeskSwitcher.exe" --tray`. Matching legacy entries are updated automatically.
- A named mutex prevents another instance in the same Windows session.

Slions.VirtualDesktop 6.9.2 is retained from the verified spike. All calls are caught and logged. A library failure uses Win+Ctrl+Left/Right through SendInput; Windows may block injection into higher-integrity applications. The native shortcut does not support optional wrapping when COM is unavailable.

Logs: `%LOCALAPPDATA%\VDeskSwitcher\app.log`.

## Manual test

1. Run/F5 without arguments: settings and the tray icon appear, but no taskbar button. Close with X: the process/icon stay alive. Reopen by tray double-click and `Nastavenia`; minimize also hides it.
2. Exit through `Ukončiť`, then run with `--tray`: no settings window should appear. A second launch still exits due to the mutex.
3. Move the slider to 100, 1500 and 5000 ms. The numeric readout/JSON should update immediately, and the next edge dwell uses that value. Restart and confirm it is restored. Test damaged JSON and values below 100/above 5000 while the app is stopped.
4. Create two or three desktops. Hold at the right edge for the chosen dwell. Confirm exactly one next-desktop switch, even if the cursor stays there.
5. Move only 19 px inward and return: no new trigger. Move 20 px inward, return and dwell: one new switch. Confirm first/last desktops do not wrap.
6. Check internal monitor boundaries, mixed display scales, held mouse buttons/dragging and fullscreen applications: existing suppression behavior must be unchanged.
7. Toggle enable/disable. Enable autostart, inspect the Run value for `--tray`, then sign in again: settings remain hidden. Disable autostart and verify the value is removed.
8. Exit through the tray; the process/icon/settings window should disappear. Review the log for COM, SendInput, settings or registry errors.

## Automated validation

`dotnet build VDeskSwitcher.slnx`

`dotnet run --project artifacts/edge-validation/EdgeValidation.csproj`

The validation harness exercises side/top timing, primary-monitor bounds, hysteresis, settings, target selection, mocked COM/fallback failures and native INPUT layout without real desktop switching or keyboard injection.
