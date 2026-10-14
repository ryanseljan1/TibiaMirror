using TibiaMirror.Geometry; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Geometry;

public class MonitorClampTests
{
    static readonly PixelRect Primary = new(0, 0, 1920, 1040);
    static readonly PixelRect Second = new(1920, 0, 2560, 1400);

    [Fact] public void Rect_fully_on_monitor_unchanged() =>
        Assert.Equal(new PixelRect(100, 100, 300, 200), MonitorClamp.Clamp(new PixelRect(100, 100, 300, 200), new[] { Primary, Second }, Primary));

    [Fact] public void Rect_on_disconnected_monitor_moves_to_primary()   // Review Focus #2
    {
        var r = MonitorClamp.Clamp(new PixelRect(5000, 300, 300, 200), new[] { Primary }, Primary);
        Assert.Equal(r, Primary.Intersect(r));   // entirely inside Primary
        Assert.Equal(300, r.Width); Assert.Equal(200, r.Height);
        Assert.True(r.X >= 0 && r.Right <= 1920 && r.Y >= 0 && r.Bottom <= 1040);
    }

    [Fact] public void Rect_mostly_offscreen_is_pulled_in_on_same_monitor()
    {
        var r = MonitorClamp.Clamp(new PixelRect(1900, 100, 300, 200), new[] { Primary }, Primary);
        Assert.Equal(new PixelRect(1620, 100, 300, 200), r);
    }

    [Fact] public void Rect_larger_than_monitor_is_shrunk_and_placed_at_origin() =>
        Assert.Equal(new PixelRect(0, 0, 1920, 1040), MonitorClamp.Clamp(new PixelRect(-50, -50, 4000, 3000), new[] { Primary }, Primary));
}
