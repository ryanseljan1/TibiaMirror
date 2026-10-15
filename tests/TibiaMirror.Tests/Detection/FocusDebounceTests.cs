using TibiaMirror.Detection; using TibiaMirror.Tests.Timers;
namespace TibiaMirror.Tests.Detection;

public class FocusDebounceTests
{
    readonly FakeClock _clock = new();
    FocusDebounce Make(bool startFocused = true) { var d = new FocusDebounce(_clock); d.Reset(startFocused); return d; }

    [Fact] public void Focused_stays_shown() => Assert.True(Make().Filter(true));

    [Fact] public void Focus_loss_hides_only_after_600ms()
    {
        var d = Make();
        Assert.True(d.Filter(false));
        _clock.Advance(0.59); Assert.True(d.Filter(false));
        _clock.Advance(0.02); Assert.False(d.Filter(false));
    }

    [Fact] public void Regaining_focus_within_window_cancels_the_hide()
    {
        var d = Make();
        d.Filter(false); _clock.Advance(0.5);
        Assert.True(d.Filter(true));
        _clock.Advance(0.5); Assert.True(d.Filter(false));    // loss timer restarted at this call
        _clock.Advance(0.5); Assert.True(d.Filter(false));
        _clock.Advance(0.2); Assert.False(d.Filter(false));
    }

    [Fact] public void Showing_is_immediate()
    {
        var d = Make(); d.Filter(false); _clock.Advance(1); Assert.False(d.Filter(false));
        Assert.True(d.Filter(true));
    }

    [Fact] public void Starting_unfocused_is_hidden_immediately() => Assert.False(Make(false).Filter(false));

    [Fact] public void Grace_after_region_change_suppresses_hide_for_2s()
    {
        var d = Make(); d.NotifyRegionChanged();
        d.Filter(false); _clock.Advance(1.9); Assert.True(d.Filter(false));
        _clock.Advance(0.2); Assert.False(d.Filter(false));
    }

    [Fact] public void Region_change_while_hidden_shows_the_mirror_for_the_grace_period()
    {
        var d = Make(false); d.NotifyRegionChanged();
        Assert.True(d.Filter(false));
        _clock.Advance(2.1); Assert.False(d.Filter(false));
    }

    [Fact] public void NextCheck_reports_time_until_pending_hide()
    {
        var d = Make();
        Assert.Null(d.NextCheck);
        d.Filter(false); _clock.Advance(0.2);
        Assert.Equal(0.4, d.NextCheck!.Value.TotalSeconds, 3);
        d.Filter(true); Assert.Null(d.NextCheck);
    }

    [Fact] public void NextCheck_accounts_for_grace()
    {
        var d = Make(); d.NotifyRegionChanged(); d.Filter(false);
        Assert.Equal(2.0, d.NextCheck!.Value.TotalSeconds, 3);
    }
}
