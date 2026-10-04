using System.IO;
using System.Text.Json;

namespace VDeskSwitcher;

internal static class EdgeSettings
{
    internal const int DefaultDwellMilliseconds = 500; // Continuous time at one edge before switching.
    internal const int MinimumDwellMilliseconds = 100;
    internal const int MaximumDwellMilliseconds = 5000;
    internal const int EdgeZonePx = 1; // Physical pixels at the outer virtual-screen edges.
    internal const int RearmDistancePx = 20; // Move this far inward before another trigger.
    internal const int PollMilliseconds = 40; // Cursor polling interval.
    internal const bool WrapAround = false; // Enable to wrap first/last desktops when COM works.

    internal static int DwellMilliseconds { get; private set; } = DefaultDwellMilliseconds;
    internal static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DeskOverview", "settings.json");

    internal static void Load(Action<string> log)
    {
        DwellMilliseconds = DefaultDwellMilliseconds;
        try
        {
            if (File.Exists(FilePath)) DwellMilliseconds = ReadDwell(File.ReadAllText(FilePath));
        }
        catch (Exception ex) { log($"SETTINGS LOAD ERROR: {AppLog.Describe(ex)}; using defaults."); }
    }

    internal static int ReadDwell(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty(nameof(DwellMilliseconds), out var value) &&
            value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long milliseconds))
            return (int)Math.Clamp(milliseconds, MinimumDwellMilliseconds, MaximumDwellMilliseconds);
        return DefaultDwellMilliseconds;
    }

    internal static void SetDwell(int milliseconds, Action<string> log)
    {
        milliseconds = Math.Clamp(milliseconds, MinimumDwellMilliseconds, MaximumDwellMilliseconds);
        if (milliseconds == DwellMilliseconds) return;
        DwellMilliseconds = milliseconds;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new { DwellMilliseconds }));
        }
        catch (Exception ex) { log($"SETTINGS SAVE ERROR: {AppLog.Describe(ex)}"); }
    }
}
