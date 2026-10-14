using TibiaMirror.Models;

namespace TibiaMirror.Geometry;

public static class CropMath
{
    public const int MinSize = 8;

    public static PixelRect FromSelection(PixelRect selectionOnScreen, PixelRect clientOnScreen)
    {
        var clipped = selectionOnScreen.Intersect(clientOnScreen);
        if (clipped.Width < MinSize || clipped.Height < MinSize) return default;
        return clipped.Offset(-clientOnScreen.X, -clientOnScreen.Y);
    }

    public static PixelRect ToThumbnailSource(PixelRect crop, PixelRect windowOnScreen, PixelRect clientOnScreen)
    {
        var clientRelative = new PixelRect(0, 0, clientOnScreen.Width, clientOnScreen.Height);
        var visible = crop.Intersect(clientRelative);
        return visible.Offset(clientOnScreen.X - windowOnScreen.X, clientOnScreen.Y - windowOnScreen.Y);
    }

    public static PixelRect ResizeKeepingAspect(PixelRect current, int proposedWidth, int proposedHeight, double aspect)
    {
        double dw = Math.Abs(proposedWidth - current.Width) / (double)Math.Max(1, current.Width);
        double dh = Math.Abs(proposedHeight - current.Height) / (double)Math.Max(1, current.Height);
        int w, h;
        if (dw >= dh) { w = proposedWidth; h = (int)Math.Round(w / aspect); }
        else { h = proposedHeight; w = (int)Math.Round(h * aspect); }
        if (h < MinSize) { h = MinSize; w = (int)Math.Round(h * aspect); }
        if (w < MinSize) { w = MinSize; h = (int)Math.Round(w / aspect); }
        return current with { Width = w, Height = h };
    }
}
