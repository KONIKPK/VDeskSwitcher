using System.Runtime.InteropServices;
using System.Text;

namespace VDeskSwitcher;

internal static class NativeMethods
{
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern nint GetShellWindow();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint hwnd, StringBuilder text, int count);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint hwnd, int attribute, out int value, int size);
    [DllImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute")] private static extern int GetFrameBounds(nint hwnd, int attribute, out Rect rect, int size);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);

    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor, Work;
        public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] private struct Input { public uint Type; public InputUnion Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion
    {
        [FieldOffset(0)] public MouseInput Mouse;
        [FieldOffset(0)] public KeyboardInput Keyboard;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput
    {
        public int X, Y;
        public uint MouseData, Flags, Time;
        public nuint ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput
    {
        public ushort Key, Scan;
        public uint Flags, Time;
        public nuint ExtraInfo;
    }

    // Native equivalents of VirtualScreenLeft/Width, in GetCursorPos physical pixels.
    internal static (int Left, int Width) VirtualScreen() => (GetSystemMetrics(76), GetSystemMetrics(78));

    private static bool KeyDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    internal static bool MouseButtonDown() => KeyDown(1) || KeyDown(2) || KeyDown(4) || KeyDown(5) || KeyDown(6);

    internal static bool IsFullscreenForeground()
    {
        nint hwnd = GetForegroundWindow();
        if (hwnd == 0 || hwnd == GetShellWindow() || !IsWindowVisible(hwnd) || IsIconic(hwnd)) return false;
        var name = new StringBuilder(128);
        GetClassName(hwnd, name, name.Capacity);
        if (name.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return false;
        if (DwmGetWindowAttribute(hwnd, 14, out int cloaked, sizeof(int)) >= 0 && cloaked != 0) return false;
        if (GetFrameBounds(hwnd, 9, out var rect, Marshal.SizeOf<Rect>()) < 0 && !GetWindowRect(hwnd, out rect)) return false;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) return false;
        return rect.Left <= info.Monitor.Left + 2 && rect.Top <= info.Monitor.Top + 2 &&
            rect.Right >= info.Monitor.Right - 2 && rect.Bottom >= info.Monitor.Bottom - 2;
    }

    internal static void SendDesktopShortcut(EdgeDirection direction)
    {
        if (direction == EdgeDirection.None) return;
        ushort arrow = direction == EdgeDirection.Next ? (ushort)0x27 : (ushort)0x25;
        bool addWin = !KeyDown(0x5B) && !KeyDown(0x5C);
        bool addControl = !KeyDown(0x11);
        bool releaseArrow = !KeyDown(arrow);
        List<Input> inputs = [];
        if (addWin) inputs.Add(Keyboard(0x5B, false));
        if (addControl) inputs.Add(Keyboard(0x11, false));
        inputs.Add(Keyboard(arrow, false));
        if (releaseArrow) inputs.Add(Keyboard(arrow, true));
        if (addControl) inputs.Add(Keyboard(0x11, true));
        if (addWin) inputs.Add(Keyboard(0x5B, true));
        uint sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
        if (sent == inputs.Count) return;
        int error = Marshal.GetLastWin32Error();
        List<ushort> held = [];
        foreach (var input in inputs.Take((int)sent))
        {
            var key = input.Data.Keyboard;
            if ((key.Flags & 2) != 0) held.Remove(key.Key);
            else if (key.Key != arrow || releaseArrow) held.Add(key.Key);
        }
        var releases = held.AsEnumerable().Reverse().Select(key => Keyboard(key, true)).ToArray();
        uint released = releases.Length == 0 ? 0 : SendInput((uint)releases.Length, releases, Marshal.SizeOf<Input>());
        throw new InvalidOperationException($"SendInput sent {sent}/{inputs.Count} events; Win32 error={error}; cleanup={released}/{releases.Length}. Windows may block injection across integrity levels (UIPI).");
    }

    private static Input Keyboard(ushort key, bool up) => new()
    {
        Type = 1,
        Data = new InputUnion { Keyboard = new KeyboardInput { Key = key,
            Flags = (key is 0x5B or 0x25 or 0x27 ? 1u : 0u) | (up ? 2u : 0u) } }
    };
}
