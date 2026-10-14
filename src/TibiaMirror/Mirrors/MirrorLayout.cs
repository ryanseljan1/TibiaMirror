using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public static class MirrorLayout
{
    /// <summary>
    /// Part of the mirror interior that corresponds to the visible part of the crop.
    /// <paramref name="clientSize"/> is (0,0,w,h) of the Tibia client area; crop is client-relative.
    /// </summary>
    public static PixelRect DestinationFor(PixelRect crop, PixelRect clientSize, PixelRect mirrorInterior)
    {
        if (crop.IsEmpty) return default;
        var visible = crop.Intersect(clientSize);
        if (visible.IsEmpty) return default;
        double fx = (visible.X - crop.X) / (double)crop.Width, fy = (visible.Y - crop.Y) / (double)crop.Height;
        double fw = visible.Width / (double)crop.Width, fh = visible.Height / (double)crop.Height;
        int x = mirrorInterior.X + (int)Math.Round(fx * mirrorInterior.Width);
        int y = mirrorInterior.Y + (int)Math.Round(fy * mirrorInterior.Height);
        int w = Math.Max(1, (int)Math.Round(fw * mirrorInterior.Width));
        int h = Math.Max(1, (int)Math.Round(fh * mirrorInterior.Height));
        return new PixelRect(x, y, w, h);
    }
}
