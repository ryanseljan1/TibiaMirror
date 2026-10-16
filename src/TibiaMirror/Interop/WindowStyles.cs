using System.Runtime.InteropServices;
using System.Windows.Interop;
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

    /// <summary>Click-through for a window without AllowsTransparency. WPF strips WS_EX_LAYERED from those (on WM_STYLECHANGING) and
    /// WS_EX_TRANSPARENT alone doesn't pass clicks through, so the window must have <see cref="KeepLayered"/> hooked first.</summary>
    public static void SetClickThroughOpaque(nint hwnd, bool enabled)
    {
        SetClickThrough(hwnd, enabled);
        if (enabled) NativeMethods.SetLayeredWindowAttributes(hwnd, 0, 255, NativeMethods.LWA_ALPHA);   // a layered window without attributes isn't drawn
    }

    /// <summary>Message hook that keeps WS_EX_LAYERED on the window while <paramref name="wanted"/> is true.</summary>
    public static HwndSourceHook KeepLayered(Func<bool> wanted) => (nint hwnd, int msg, nint wParam, nint lParam, ref bool handled) =>
    {
        if (msg != NativeMethods.WM_STYLECHANGING || wParam != NativeMethods.GWL_EXSTYLE || !wanted()) return 0;
        var s = Marshal.PtrToStructure<NativeMethods.STYLESTRUCT>(lParam);
        s.styleNew |= (uint)NativeMethods.WS_EX_LAYERED;
        Marshal.StructureToPtr(s, lParam, false);
        handled = true;   // keeps WPF's own handler from stripping it again
        return 0;
    };

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
