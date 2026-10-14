using TibiaMirror.Models;

namespace TibiaMirror.Countdown;

public static class CountdownBarLayout
{
    const int NarrowMin = 24;

    /// <summary>Bar rectangle just outside the mirror's given edge (touching it), in the mirror's coordinate space.</summary>
    public static PixelRect Compute(PixelRect mirror, BarSide side, BarShape shape, int thickness)
    {
        bool horizontal = side is BarSide.Top or BarSide.Bottom;
        int edge = horizontal ? mirror.Width : mirror.Height;
        int length = edge, offset = 0;
        if (shape == BarShape.NarrowStrip)
        {
            length = Math.Min(edge, Math.Max(NarrowMin, edge * 25 / 100));
            offset = (edge - length) / 2;
        }
        return side switch
        {
            BarSide.Top => new PixelRect(mirror.X + offset, mirror.Y - thickness, length, thickness),
            BarSide.Bottom => new PixelRect(mirror.X + offset, mirror.Bottom, length, thickness),
            BarSide.Left => new PixelRect(mirror.X - thickness, mirror.Y + offset, thickness, length),
            _ => new PixelRect(mirror.Right, mirror.Y + offset, thickness, length),
        };
    }
}
