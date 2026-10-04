using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace VDeskSwitcher;

internal sealed class TrayController : IDisposable
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "VDeskSwitcher";
    private static string ExecutablePath => Path.Combine(AppContext.BaseDirectory, "VDeskSwitcher.exe");
    private static string AutostartCommand => $"\"{ExecutablePath}\" --tray";
    private readonly Forms.NotifyIcon tray;
    private readonly Forms.ContextMenuStrip menu;
    private readonly Icon icon;
    private readonly Forms.ToolStripMenuItem toggle;
    private readonly Forms.ToolStripMenuItem autostart;
    private readonly DispatcherTimer cursorTimer;
    private readonly DesktopSwitcher switcher;
    private readonly EdgeDwell dwell = new();
    private readonly Action<string> log;
    private SettingsWindow? settings;
    private bool enabled = true;
    private bool disposed;

    internal TrayController(Action<string> log)
    {
        this.log = log;
        switcher = new DesktopSwitcher(log);
        using var iconStream = typeof(TrayController).Assembly.GetManifestResourceStream("DeskOverview.ico")
            ?? throw new InvalidOperationException("Embedded tray icon is missing.");
        using var trayIcon = new Icon(iconStream, Forms.SystemInformation.SmallIconSize);
        icon = (Icon)trayIcon.Clone();
        menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Nastavenia", null, (_, _) => ShowSettings());
        toggle = new Forms.ToolStripMenuItem("Vypnúť");
        toggle.Click += ToggleEnabled;
        menu.Items.Add(toggle);
        autostart = new Forms.ToolStripMenuItem("Spustiť pri štarte Windows");
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
            var command = key?.GetValue(RunValue) as string;
            autostart.Checked = command is not null;
            if (string.Equals(command, $"\"{ExecutablePath}\"", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(command, ExecutablePath, StringComparison.OrdinalIgnoreCase))
                key!.SetValue(RunValue, AutostartCommand, RegistryValueKind.String);
        }
        catch (Exception ex) { log($"AUTOSTART READ ERROR: {AppLog.Describe(ex)}"); }
        autostart.Click += ToggleAutostart;
        menu.Items.Add(autostart);
        menu.Items.Add("Ukončiť", null, (_, _) => Application.Current.Shutdown());
        tray = new Forms.NotifyIcon { Icon = icon, Text = "DeskOverview", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => ShowSettings();
        cursorTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(EdgeSettings.PollMilliseconds)
        };
        cursorTimer.Tick += CursorTick;
        cursorTimer.Start();
        log($"TRAY READY: poll={EdgeSettings.PollMilliseconds} ms, dwell={EdgeSettings.DwellMilliseconds} ms, edge={EdgeSettings.EdgeZonePx} px, rearm={EdgeSettings.RearmDistancePx} px, wrap={EdgeSettings.WrapAround}.");
    }

    internal void ShowSettings()
    {
        if (disposed) return;
        if (settings is null)
        {
            settings = new SettingsWindow(log);
            settings.Closed += (_, _) => settings = null;
        }
        settings.WindowState = WindowState.Normal;
        settings.Show();
        settings.Activate();
    }

    private void ToggleAutostart(object? sender, EventArgs e)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKey);
            if (autostart.Checked) key.DeleteValue(RunValue, false);
            else
            {
                if (!File.Exists(ExecutablePath)) throw new InvalidOperationException("Autostart requires the VDeskSwitcher.exe app host.");
                key.SetValue(RunValue, AutostartCommand, RegistryValueKind.String);
            }
            autostart.Checked = !autostart.Checked;
            log($"AUTOSTART: {autostart.Checked}");
        }
        catch (Exception ex) { log($"AUTOSTART ERROR: {AppLog.Describe(ex)}"); }
    }

    private void ToggleEnabled(object? sender, EventArgs e)
    {
        enabled = !enabled;
        dwell.CancelPending();
        cursorTimer.IsEnabled = enabled;
        toggle.Text = enabled ? "Vypnúť" : "Zapnúť";
        tray.Text = enabled ? "DeskOverview" : "DeskOverview — vypnuté";
        log($"ENABLED: {enabled}");
    }

    private void CursorTick(object? sender, EventArgs e)
    {
        if (!enabled || disposed) return;
        try
        {
            if (!NativeMethods.GetCursorPos(out var cursor)) { dwell.CancelPending(); return; }
            var (left, width) = NativeMethods.VirtualScreen();
            long right = (long)left + width - 1;
            bool atEdge = (cursor.X >= left && cursor.X < (long)left + EdgeSettings.EdgeZonePx) ||
                (cursor.X <= right && cursor.X > right - EdgeSettings.EdgeZonePx);
            bool suppressed = NativeMethods.MouseButtonDown() || (atEdge && NativeMethods.IsFullscreenForeground());
            var direction = dwell.Update(Environment.TickCount64, cursor.X, left, width, suppressed);
            if (direction != EdgeDirection.None) switcher.Switch(direction);
        }
        catch (Exception ex)
        {
            dwell.CancelPending();
            log($"CURSOR CHECK ERROR: {AppLog.Describe(ex)}");
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        cursorTimer.Stop();
        cursorTimer.Tick -= CursorTick;
        settings?.ClosePermanently();
        tray.Visible = false;
        tray.Dispose();
        menu.Dispose();
        icon.Dispose();
    }
}
