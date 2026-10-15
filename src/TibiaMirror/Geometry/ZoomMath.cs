using TibiaMirror.Models;

namespace TibiaMirror.Geometry;

/// <summary>Pure maths for mouse-wheel zoom of a mirror window, relative to the crop's native pixel size.</summary>
public static class ZoomMath
{
    public const double MinScale = 0.5;
    public const double MaxScale = 4.0;
    public const double Step = 0.1;

    /// <summary>Current window scale: window width over crop width (1.0 = native pixels).</summary>
    public static double ScaleOf(PixelRect window, PixelRect crop) =>
        crop.Width <= 0 ? 1.0 : window.Width / (double)crop.Width;

    /// <summary>Moves <paramref name="notches"/> steps from <paramref name="scale"/>, snapping to the 0.1 grid and clamping to 0.5-4.</summary>
    public static double NextScale(double scale, int notches) =>
        Math.Clamp(Math.Round(scale + notches * Step, 1, MidpointRounding.AwayFromZero), MinScale, MaxScale);

    /// <summary>Whole notches for a wheel delta (120 per notch); partial notches from smooth wheels count as one step.</summary>
    public static int NotchesFromDelta(int delta) =>
        delta == 0 ? 0 : Math.Sign(delta) * Math.Max(1, Math.Abs(delta) / 120);

    /// <summary>
    /// New window rectangle after zooming. Keeps the crop's aspect ratio; the point at fraction
    /// (<paramref name="anchorX"/>, <paramref name="anchorY"/>) of the window stays fixed (0,0 = top-left).
    /// </summary>
    public static PixelRect Zoom(PixelRect window, PixelRect crop, int notches, double anchorX = 0, double anchorY = 0)
    {
        if (crop.IsEmpty || window.IsEmpty) return window;
        var scale = NextScale(ScaleOf(window, crop), notches);
        int w = Math.Max(CropMath.MinSize, (int)Math.Round(crop.Width * scale));
        int h = Math.Max(CropMath.MinSize, (int)Math.Round(crop.Height * scale));
        anchorX = Math.Clamp(anchorX, 0, 1); anchorY = Math.Clamp(anchorY, 0, 1);
        int x = window.X + (int)Math.Round(anchorX * (window.Width - w));
        int y = window.Y + (int)Math.Round(anchorY * (window.Height - h));
        return new PixelRect(x, y, w, h);
    }
}
