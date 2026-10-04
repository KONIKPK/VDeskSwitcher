using System.Windows;

namespace VDeskSwitcher;

public sealed class App : Application
{
    private TrayController? tray;
    private readonly AppLog log = new();
    private Mutex? instanceMutex;
    private bool ownsMutex;

    [STAThread]
    public static int Main() => new App { ShutdownMode = ShutdownMode.OnExplicitShutdown }.Run();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            instanceMutex = new Mutex(false, @"Local\VDeskSwitcher.EdgeSwitcher");
            try { ownsMutex = instanceMutex.WaitOne(0); }
            catch (AbandonedMutexException) { ownsMutex = true; }
            if (!ownsMutex)
            {
                log.Write("Another instance is already running; exiting.");
                Shutdown(0);
                return;
            }
            log.Write($"START: DeskOverview edge switcher; Windows={Environment.OSVersion.Version}; log={log.FilePath}");
            EdgeSettings.Load(log.Write);
            tray = new TrayController(log.Write);
            if (!e.Args.Contains("--tray", StringComparer.OrdinalIgnoreCase)) tray.ShowSettings();
        }
        catch (Exception ex)
        {
            log.Write($"STARTUP ERROR: {AppLog.Describe(ex)}");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        tray?.Dispose();
        if (ownsMutex) instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
