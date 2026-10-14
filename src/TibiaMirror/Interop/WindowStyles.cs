using TibiaMirror.Models;

namespace TibiaMirror.Interop;

public static class WindowStyles
{
    public static void MakeToolWindow(nint hwnd)
    {
        var ex = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE,
            ex | (nint)(NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE));
    }

    public static void SetClickThrough(nint hwnd, bool enabled)
    {
        var ex = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, (nint)ComputeExStyle(ex, enabled));
    }

    /// <summary>Enable adds TRANSPARENT|LAYERED; disable clears only TRANSPARENT (WPF owns LAYERED).</summary>
    internal static long ComputeExStyle(long current, bool clickThrough) =>
        clickThrough
            ? current | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_LAYERED
            : current & ~NativeMethods.WS_EX_TRANSPARENT;

    public static void SetBoundsPhysical(nint hwnd, PixelRect rect) =>
        NativeMethods.SetWindowPos(hwnd, NativeMethods.HWND_TOPMOST, rect.X, rect.Y, rect.Width, rect.Height, NativeMethods.SWP_NOACTIVATE);

    public static PixelRect GetBoundsPhysical(nint hwnd) =>
        NativeMethods.GetWindowRect(hwnd, out var r) ? new PixelRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top) : default;
}
