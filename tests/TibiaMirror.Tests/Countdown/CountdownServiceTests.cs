using TibiaMirror.Countdown; using TibiaMirror.Models; using TibiaMirror.Tests.Timers;
namespace TibiaMirror.Tests.Countdown;

public class CountdownServiceTests
{
    readonly FakeClock _clock = new(); readonly Guid _id = Guid.NewGuid();
    [Fact] public void Progress_drains_linearly()
    {
        var s = new CountdownService(_clock); s.Start(_id, 10);
        Assert.Equal(1.0, s.Progress(_id), 3); _clock.Advance(2.5); Assert.Equal(0.75, s.Progress(_id), 3);
    }
    [Fact] public void Completes_once()
    {
        var s = new CountdownService(_clock); int n = 0; s.Completed += _ => n++;
        s.Start(_id, 1); _clock.Advance(1.01); s.Tick(); s.Tick();
        Assert.Equal(1, n); Assert.False(s.IsRunning(_id)); Assert.Equal(0, s.Progress(_id));
    }
    [Fact] public void Restart_refills()
    {
        var s = new CountdownService(_clock); s.Start(_id, 10); _clock.Advance(8); s.Start(_id, 10);
        Assert.Equal(1.0, s.Progress(_id), 3);
    }
    [Fact] public void Stop_prevents_completion() { var s = new CountdownService(_clock); int n = 0; s.Completed += _ => n++; s.Start(_id, 1); s.Stop(_id); _clock.Advance(5); s.Tick(); Assert.Equal(0, n); }
}

public class CountdownBarLayoutTests
{
    static readonly PixelRect M = new(100, 100, 200, 100);
    [Fact] public void Bottom_full() => Assert.Equal(new PixelRect(100, 200, 200, 6), CountdownBarLayout.Compute(M, BarSide.Bottom, BarShape.FullEdge, 6));
    [Fact] public void Top_full() => Assert.Equal(new PixelRect(100, 94, 200, 6), CountdownBarLayout.Compute(M, BarSide.Top, BarShape.FullEdge, 6));
    [Fact] public void Left_full() => Assert.Equal(new PixelRect(94, 100, 6, 100), CountdownBarLayout.Compute(M, BarSide.Left, BarShape.FullEdge, 6));
    [Fact] public void Right_narrow_is_centered_quarter() => Assert.Equal(new PixelRect(300, 137, 6, 25), CountdownBarLayout.Compute(M, BarSide.Right, BarShape.NarrowStrip, 6));
    [Fact] public void Narrow_has_minimum_length() => Assert.Equal(24, CountdownBarLayout.Compute(new PixelRect(0, 0, 40, 40), BarSide.Bottom, BarShape.NarrowStrip, 4).Width);
}
