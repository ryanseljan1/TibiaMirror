using TibiaMirror.Models;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class MainWindowBoundsPolicyTests
{
    static readonly PixelRect Primary = new(0, 0, 1920, 1040);
    static readonly IReadOnlyList<PixelRect> Areas = new[] { Primary, new PixelRect(1920, 0, 1920, 1040) };
    static PixelRect Clamp(PixelRect r) => RestoreClamp.Clamp(r, Areas, Primary);

    [Fact]
    public void Target_is_null_without_usable_saved_bounds()
    {
        Assert.Null(MainWindowBoundsPolicy.Target(null, Clamp));
        Assert.Null(MainWindowBoundsPolicy.Target(new PixelRect(0, 0, 0, 0), Clamp));
    }

    [Fact]
    public void Target_keeps_on_screen_bounds_on_a_secondary_monitor()
    {
        var saved = new PixelRect(2500, 250, 520, 640);
        Assert.Equal(saved, MainWindowBoundsPolicy.Target(saved, Clamp));
    }

    [Fact]
    public void Target_moves_off_screen_bounds_onto_a_monitor()
    {
        var t = MainWindowBoundsPolicy.Target(new PixelRect(9000, 9000, 520, 640), Clamp)!.Value;
        Assert.Contains(Areas, a => a.Intersect(t) == t);
    }

    [Fact]
    public void NeedsApply_only_when_actual_differs()
    {
        var target = new PixelRect(1500, 250, 520, 640);
        Assert.False(MainWindowBoundsPolicy.NeedsApply(target, target));
        Assert.True(MainWindowBoundsPolicy.NeedsApply(target, new PixelRect(130, 234, 520, 640)));
    }

    [Fact]
    public void ShouldSave_is_blocked_until_restore_is_complete()
    {
        var saved = new PixelRect(1500, 250, 520, 640);
        var wpfDefault = new PixelRect(130, 234, 520, 640);
        Assert.False(MainWindowBoundsPolicy.ShouldSave(false, wpfDefault, saved));
        Assert.True(MainWindowBoundsPolicy.ShouldSave(true, wpfDefault, saved));
        Assert.False(MainWindowBoundsPolicy.ShouldSave(true, saved, saved));
        Assert.False(MainWindowBoundsPolicy.ShouldSave(true, default, saved));
    }
}
