using System.Runtime.InteropServices;
using System.Text;

namespace TibiaMirror.Interop;

internal static class NativeMethods
{
    public delegate bool EnumWindowsProc(nint hwnd, nint lParam);
    public delegate bool MonitorEnumProc(nint hMonitor, nint hdc, nint lprcMonitor, nint dwData);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_THUMBNAIL_PROPERTIES
    {
        public uint dwFlags;
        public RECT rcDestination;
        public RECT rcSource;
        public byte opacity;
        [MarshalAs(UnmanagedType.Bool)] public bool fVisible;
        [MarshalAs(UnmanagedType.Bool)] public bool fSourceClientAreaOnly;
    }

    public const uint DWM_TNP_RECTDESTINATION = 1;
    public const uint DWM_TNP_RECTSOURCE = 2;
    public const uint DWM_TNP_OPACITY = 4;
    public const uint DWM_TNP_VISIBLE = 8;
    public const uint DWM_TNP_SOURCECLIENTAREAONLY = 0x10;

    public const int GWL_EXSTYLE = -20;
    public const long WS_EX_TRANSPARENT = 0x20;
    public const long WS_EX_TOOLWINDOW = 0x80;
    public const long WS_EX_LAYERED = 0x80000;
    public const long WS_EX_NOACTIVATE = 0x08000000;

    public static readonly nint HWND_TOPMOST = -1;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint MONITOR_DEFAULTTOPRIMARY = 1;

    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, nint lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(nint hwnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(nint hwnd);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] public static extern bool IsIconic(nint hwnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(nint hwnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(nint hwnd, ref POINT pt);
    [DllImport("user32.dll")] public static extern nint GetForegroundWindow();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] public static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] public static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] public static extern bool EnumDisplayMonitors(nint hdc, nint clip, MonitorEnumProc cb, nint data);
    [DllImport("user32.dll")] public static extern bool GetMonitorInfo(nint hMonitor, ref MONITORINFO info);
    [DllImport("user32.dll")] public static extern nint MonitorFromWindow(nint hwnd, uint flags);

    [DllImport("dwmapi.dll")] public static extern int DwmRegisterThumbnail(nint dest, nint src, out nint thumb);
    [DllImport("dwmapi.dll")] public static extern int DwmUnregisterThumbnail(nint thumb);
    [DllImport("dwmapi.dll")] public static extern int DwmUpdateThumbnailProperties(nint thumb, ref DWM_THUMBNAIL_PROPERTIES props);
    [DllImport("dwmapi.dll")] public static extern int DwmQueryThumbnailSourceSize(nint thumb, out POINT size);
}
