using TibiaMirror.Hotkeys; using TibiaMirror.Models; using TibiaMirror.Mirrors;
namespace TibiaMirror.Tests.Hotkeys;

public class GlobalHotkeyOwnerTests
{
    [Fact] public void Conflict_reports_owner_with_friendly_name()
    {
        using var s = new HotkeyService(new FakeKeyboard(), () => true, a => a());
        var owner = new GlobalHotkeyOwner("Show/Hide all mirrors");
        s.Register(owner, new Hotkey(0x70, 0), HotkeyScope.Global, () => { });
        var others = s.FindConflicts(new Hotkey(0x70, 0), new object());
        Assert.Equal("Show/Hide all mirrors", Assert.Single(others).ToString());
        Assert.Empty(s.FindConflicts(new Hotkey(0x70, 0), owner));
    }

    [Theory]
    [InlineData(new string[0], "Region 1")]
    [InlineData(new[] { "Region 1", "Region 2", "Region 3" }, "Region 4")]
    [InlineData(new[] { "Region 1", "Region 3" }, "Region 2")]
    [InlineData(new[] { "Mine", "Region 2" }, "Region 1")]
    [InlineData(new[] { "region 1" }, "Region 2")]
    public void RegionNaming_picks_lowest_unused(string[] existing, string expected) =>
        Assert.Equal(expected, RegionNaming.NextName(existing));
}

public class CountdownSoundWarningTests
{
    [Fact] public void No_sound_or_playable_sound_has_no_warning()
    {
        Assert.Equal("", TibiaMirror.Countdown.CountdownSoundWarning.For(null, _ => false));
        Assert.Equal("", TibiaMirror.Countdown.CountdownSoundWarning.For("builtin:chime", _ => true));
    }
    [Fact] public void Missing_sound_warns_and_names_the_fallback() =>
        Assert.StartsWith("Sound file not found", TibiaMirror.Countdown.CountdownSoundWarning.For(@"C:\gone.wav", _ => false));
}
