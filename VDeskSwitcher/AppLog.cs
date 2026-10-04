using System.Diagnostics;
using System.IO;

namespace VDeskSwitcher;

internal sealed class AppLog
{
    internal string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VDeskSwitcher", "app.log");

    internal void Write(string message)
    {
        string line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.AppendAllText(FilePath, line);
        }
        catch (Exception ex) { Trace.WriteLine($"{line}Log write failed: {ex.Message}"); }
    }

    internal static string Describe(Exception exception)
    {
        var root = exception.GetBaseException();
        return $"{root.GetType().Name}, HRESULT=0x{root.HResult:X8}: {root.Message}";
    }
}
