using TibiaMirror.Controls; using TibiaMirror.Tests.Timers;
namespace TibiaMirror.Tests.Controls;

public class SnackbarServiceTests
{
    readonly FakeClock _clock = new();
    SnackbarService Make() => new(_clock);

    [Fact] public void Show_makes_message_visible()
    {
        var s = Make(); s.Show("Mirror deleted", () => { });
        Assert.True(s.IsVisible); Assert.Equal("Mirror deleted", s.Message);
    }

    [Fact] public void Hides_after_five_seconds()
    {
        var s = Make(); s.Show("x", () => { });
        _clock.Advance(4.9); s.Tick(); Assert.True(s.IsVisible);
        _clock.Advance(0.2); s.Tick(); Assert.False(s.IsVisible);
    }

    [Fact] public void Undo_runs_action_once_and_hides()
    {
        var s = Make(); int n = 0; s.Show("x", () => n++);
        s.Undo(); s.Undo();
        Assert.Equal(1, n); Assert.False(s.IsVisible);
    }

    [Fact] public void Undo_after_expiry_does_nothing()
    {
        var s = Make(); int n = 0; s.Show("x", () => n++);
        _clock.Advance(6); s.Tick(); s.Undo();
        Assert.Equal(0, n);
    }

    [Fact] public void Undo_after_expiry_without_tick_does_nothing()
    {
        var s = Make(); int n = 0; s.Show("x", () => n++);
        _clock.Advance(6); s.Undo();
        Assert.Equal(0, n); Assert.False(s.IsVisible);
    }

    [Fact] public void New_snackbar_replaces_old_and_old_undo_is_gone()
    {
        var s = Make(); int a = 0, b = 0;
        s.Show("first", () => a++); _clock.Advance(3);
        s.Show("second", () => b++);
        Assert.Equal("second", s.Message);
        _clock.Advance(3); s.Tick(); Assert.True(s.IsVisible);   // timer restarted by the second one
        s.Undo();
        Assert.Equal(0, a); Assert.Equal(1, b);
    }

    [Fact] public void Dismiss_hides_without_undoing()
    {
        var s = Make(); int n = 0; s.Show("x", () => n++);
        s.Dismiss(); s.Undo();
        Assert.False(s.IsVisible); Assert.Equal(0, n);
    }

    [Fact] public void Failing_undo_is_swallowed_and_hides()
    {
        var s = Make(); s.Show("x", () => throw new InvalidOperationException("boom"));
        s.Undo();
        Assert.False(s.IsVisible);
    }

    [Fact] public void Raises_property_changed_for_visibility()
    {
        var s = Make(); var seen = new List<string?>(); s.PropertyChanged += (_, e) => seen.Add(e.PropertyName);
        s.Show("x", () => { });
        Assert.Contains(nameof(SnackbarService.IsVisible), seen); Assert.Contains(nameof(SnackbarService.Message), seen);
    }
}

public class SnackbarServiceActionsTests
{
    readonly FakeClock _clock = new();
    SnackbarService Make() => new(_clock);

    [Fact] public void Holds_n_actions()
    {
        var s = Make();
        s.Show("m", [new("A", () => { }), new("B", () => { }), new("C", () => { })], TimeSpan.FromSeconds(5));
        Assert.Equal(["A", "B", "C"], s.Actions.Select(a => a.Label));
    }

    [Fact] public void Sticky_never_expires()
    {
        var s = Make(); s.Show("m", [], null);
        _clock.Advance(3600); s.Tick();
        Assert.True(s.IsVisible);
    }

    [Fact] public void Custom_duration_expires()
    {
        var s = Make(); s.Show("m", [], TimeSpan.FromSeconds(20));
        _clock.Advance(19); s.Tick(); Assert.True(s.IsVisible);
        _clock.Advance(2); s.Tick(); Assert.False(s.IsVisible);
    }

    [Fact] public void Action_runs_once_and_dismisses()
    {
        var s = Make(); int n = 0; var a = new SnackbarAction("Go", () => n++);
        s.Show("m", [a], null);
        s.RunAction(a); s.RunAction(a);
        Assert.Equal(1, n); Assert.False(s.IsVisible); Assert.Empty(s.Actions);
    }

    [Fact] public void Failing_action_is_swallowed()
    {
        var s = Make(); var a = new SnackbarAction("Go", () => throw new InvalidOperationException());
        s.Show("m", [a], null); s.RunAction(a);
        Assert.False(s.IsVisible);
    }

    [Fact] public void Progress_and_busy_reset_on_show_and_dismiss()
    {
        var s = Make(); s.Show("m", [], null);
        s.IsBusy = true; s.Progress = 0.4;
        Assert.False(s.IsIndeterminate);
        s.Show("n", [], null);
        Assert.False(s.IsBusy); Assert.Null(s.Progress); Assert.True(s.IsIndeterminate);
        s.Progress = 1; s.Dismiss(); Assert.Null(s.Progress);
    }
}
