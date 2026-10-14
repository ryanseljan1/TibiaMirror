using System.Runtime.InteropServices;
using TibiaMirror.Models;

namespace TibiaMirror.Interop;

public static class Monitors
{
    public static IReadOnlyList<PixelRect> WorkAreas()
    {
        var list = new List<PixelRect>();
        NativeMethods.EnumDisplayMonitors(0, 0, (m, _, _, _) =>
        {
            if (TryInfo(m, out var info)) list.Add(ToRect(info.rcWork));
            return true;
        }, 0);
        return list;
    }

    public static PixelRect PrimaryWorkArea()
    {
        var m = NativeMethods.MonitorFromWindow(0, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        return TryInfo(m, out var info) ? ToRect(info.rcWork) : new PixelRect(0, 0, 1920, 1080);
    }

    public static uint DpiForWindow(nint hwnd)
    {
        var dpi = NativeMethods.GetDpiForWindow(hwnd);
        return dpi == 0 ? 96 : dpi;
    }

    static bool TryInfo(nint monitor, out NativeMethods.MONITORINFO info)
    {
        info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        return NativeMethods.GetMonitorInfo(monitor, ref info);
    }

    static PixelRect ToRect(NativeMethods.RECT r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
}
