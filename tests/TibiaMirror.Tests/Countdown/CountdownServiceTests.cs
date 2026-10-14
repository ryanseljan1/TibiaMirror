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
