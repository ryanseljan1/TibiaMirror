using System.ComponentModel;
using TibiaMirror.Models;

namespace TibiaMirror.Interop;

public sealed class DwmThumbnail : IDisposable
{
    nint _thumb;

    public DwmThumbnail(nint destinationHwnd, nint sourceHwnd)
    {
        int hr = NativeMethods.DwmRegisterThumbnail(destinationHwnd, sourceHwnd, out _thumb);
        if (hr < 0) throw new Win32Exception(hr, $"DwmRegisterThumbnail failed (0x{hr:X8})");
    }

    /// <param name="destination">Relative to the destination window client area, physical px.</param>
    public void Update(PixelRect source, PixelRect destination, double opacity, bool visible)
    {
        if (_thumb == 0) return;
        var props = new NativeMethods.DWM_THUMBNAIL_PROPERTIES
        {
            dwFlags = NativeMethods.DWM_TNP_RECTDESTINATION | NativeMethods.DWM_TNP_RECTSOURCE | NativeMethods.DWM_TNP_OPACITY
                      | NativeMethods.DWM_TNP_VISIBLE | NativeMethods.DWM_TNP_SOURCECLIENTAREAONLY,
            rcDestination = new NativeMethods.RECT { Left = destination.X, Top = destination.Y, Right = destination.Right, Bottom = destination.Bottom },
            rcSource = new NativeMethods.RECT { Left = source.X, Top = source.Y, Right = source.Right, Bottom = source.Bottom },
            opacity = (byte)Math.Round(Math.Clamp(opacity, 0.1, 1) * 255),
            fVisible = visible,
            fSourceClientAreaOnly = false,
        };
        int hr = NativeMethods.DwmUpdateThumbnailProperties(_thumb, ref props);
        if (hr < 0) throw new Win32Exception(hr, $"DwmUpdateThumbnailProperties failed (0x{hr:X8})");
    }

    public void Dispose()
    {
        if (_thumb == 0) return;
        NativeMethods.DwmUnregisterThumbnail(_thumb);
        _thumb = 0;
    }
}
