using TibiaMirror.Abstractions; using TibiaMirror.Detection; using TibiaMirror.Mirrors; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Mirrors;

public class MirrorVisibilityTests
{
    static readonly WindowInfo W = new(1, "Tibia", "client", true, false, new(0, 0, 800, 600), new(8, 31, 784, 561));
    static TibiaState Found(bool focused = true, bool min = false) => new(W with { IsMinimized = min }, focused);
    static readonly RegionModel R = new() { IsVisible = true };

    [Fact] public void Shown_when_tibia_found() => Assert.True(MirrorVisibility.ShouldShow(R, Found(), false, false));
    [Fact] public void Hidden_when_tibia_missing() => Assert.False(MirrorVisibility.ShouldShow(R, new TibiaState(null, false), false, false));
    [Fact] public void Hidden_when_tibia_minimized() => Assert.False(MirrorVisibility.ShouldShow(R, Found(min: true), false, false));
    [Fact] public void Hidden_when_all_hidden() => Assert.False(MirrorVisibility.ShouldShow(R, Found(), true, false));
    [Fact] public void Hidden_when_region_hidden() => Assert.False(MirrorVisibility.ShouldShow(new RegionModel { IsVisible = false }, Found(), false, false));
    [Fact] public void Focus_rule_applies_only_when_enabled()
    {
        Assert.True(MirrorVisibility.ShouldShow(R, Found(focused: false), false, false));
        Assert.False(MirrorVisibility.ShouldShow(R, Found(focused: false), false, true));
    }

    [Fact] public void Locked_mirror_is_always_click_through()
    {
        var locked = new RegionModel { IsLocked = true };
        Assert.True(MirrorVisibility.IsClickThrough(locked, tibiaFocused: false, clickThroughWhenFocused: false));
        Assert.True(MirrorVisibility.IsClickThrough(locked, tibiaFocused: true, clickThroughWhenFocused: true));
    }

    [Fact] public void Unlocked_mirror_is_click_through_only_while_tibia_is_focused_and_the_setting_is_on()
    {
        Assert.True(MirrorVisibility.IsClickThrough(R, tibiaFocused: true, clickThroughWhenFocused: true));
        Assert.False(MirrorVisibility.IsClickThrough(R, tibiaFocused: false, clickThroughWhenFocused: true));
        Assert.False(MirrorVisibility.IsClickThrough(R, tibiaFocused: true, clickThroughWhenFocused: false));
    }

    [Fact] public void Click_through_while_focused_is_on_by_default() => Assert.True(new AppSettings().ClickThroughWhileTibiaFocused);
}
