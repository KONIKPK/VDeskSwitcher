using WindowsDesktop;

namespace VDeskSwitcher;

internal sealed class DesktopSwitcher
{
    private readonly Action<string> log;
    private readonly Action<EdgeDirection> librarySwitch;
    private readonly Action<EdgeDirection> shortcutSwitch;

    internal DesktopSwitcher(Action<string> log, Action<EdgeDirection>? librarySwitch = null,
        Action<EdgeDirection>? shortcutSwitch = null)
    {
        this.log = log;
        this.librarySwitch = librarySwitch ?? SwitchUsingLibrary;
        this.shortcutSwitch = shortcutSwitch ?? NativeMethods.SendDesktopShortcut;
    }

    internal void Switch(EdgeDirection direction)
    {
        if (direction == EdgeDirection.None) return;
        try
        {
            librarySwitch(direction);
            return;
        }
        catch (Exception ex) { log($"DESKTOP LIBRARY ERROR: {AppLog.Describe(ex)}; using Win+Ctrl+Arrow fallback."); }
        try
        {
            shortcutSwitch(direction);
            log($"SENDINPUT: requested {direction}; native shortcut does not wrap desktops.");
        }
        catch (Exception ex) { log($"SENDINPUT ERROR: {AppLog.Describe(ex)}"); }
    }

    private void SwitchUsingLibrary(EdgeDirection direction)
    {
        VirtualDesktop.Configure();
        var desktops = VirtualDesktop.GetDesktops();
        var current = VirtualDesktop.Current.Id;
        int index = Array.FindIndex(desktops, desktop => desktop.Id == current);
        if (index < 0) throw new InvalidOperationException("Current desktop was not found in the desktop list.");
        int target = TargetIndex(index, desktops.Length, direction, EdgeSettings.WrapAround);
        if (target < 0 || target == index)
        {
            log($"DESKTOP BOUNDARY: {direction}; no switch.");
            return;
        }
        desktops[target].Switch();
        log($"DESKTOP SWITCH REQUESTED: {index + 1} -> {target + 1} ({direction}).");
    }

    internal static int TargetIndex(int current, int count, EdgeDirection direction, bool wrapAround)
    {
        if (count <= 0 || current < 0 || current >= count || direction == EdgeDirection.None) return -1;
        int target = current + (int)direction;
        if (target >= 0 && target < count) return target;
        return wrapAround ? (target + count) % count : -1;
    }
}
