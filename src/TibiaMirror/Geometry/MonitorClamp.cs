using TibiaMirror.Models;

namespace TibiaMirror.Geometry;

public static class MonitorClamp
{
    public static PixelRect Clamp(PixelRect rect, IReadOnlyList<PixelRect> workAreas, PixelRect primaryWorkArea)
    {
        PixelRect area = default;
        long best = 0;
        foreach (var wa in workAreas)
        {
            var i = rect.Intersect(wa);
            long size = (long)i.Width * i.Height;
            if (!i.IsEmpty && size > best) { best = size; area = wa; }
        }
        if (best == 0)
        {
            area = primaryWorkArea;
            rect = rect.Offset(primaryWorkArea.X + 40 - rect.X, primaryWorkArea.Y + 40 - rect.Y);
        }

        int w = Math.Min(rect.Width, area.Width);
        int h = Math.Min(rect.Height, area.Height);
        int x = Math.Clamp(rect.X, area.X, area.Right - w);
        int y = Math.Clamp(rect.Y, area.Y, area.Bottom - h);
        return new PixelRect(x, y, w, h);
    }
}
