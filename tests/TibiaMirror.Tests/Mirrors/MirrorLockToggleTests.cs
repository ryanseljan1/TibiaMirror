using TibiaMirror.Mirrors; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Mirrors;

public class MirrorLockToggleTests
{
    static RegionModel R(bool visible, bool locked) => new() { IsVisible = visible, IsLocked = locked };

    [Fact] public void Locks_when_any_visible_mirror_is_unlocked() => Assert.True(MirrorLockToggle.ShouldLock(new[] { R(true, true), R(true, false) }));
    [Fact] public void Unlocks_when_all_visible_mirrors_are_locked() => Assert.False(MirrorLockToggle.ShouldLock(new[] { R(true, true), R(true, true) }));
    [Fact] public void Hidden_unlocked_mirror_is_ignored() => Assert.False(MirrorLockToggle.ShouldLock(new[] { R(true, true), R(false, false) }));
    [Fact] public void No_mirrors_means_unlock() => Assert.False(MirrorLockToggle.ShouldLock(Array.Empty<RegionModel>()));
    [Fact] public void Button_reads_Lock_all_with_no_mirrors() { Assert.True(MirrorLockToggle.ShowsLock(Array.Empty<RegionModel>())); Assert.False(MirrorLockToggle.ShowsLock(new[] { R(false, true) })); Assert.False(MirrorLockToggle.ShowsLock(new[] { R(true, true) })); }

    [Fact] public void Label_says_Lock_all_when_next_toggle_locks() => Assert.Equal("Lock all", TibiaMirror.Localization.Loc.Get(MirrorLockToggle.LabelKey(true)));
    [Fact] public void Label_says_Unlock_all_when_next_toggle_unlocks() => Assert.Equal("Unlock all", TibiaMirror.Localization.Loc.Get(MirrorLockToggle.LabelKey(false)));
}
