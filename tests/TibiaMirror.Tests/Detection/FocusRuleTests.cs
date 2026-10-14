using TibiaMirror.Detection;

namespace TibiaMirror.Tests.Detection;

public class FocusRuleTests
{
    static readonly HashSet<nint> Mirrors = new() { 20, 21 };

    [Fact] public void Tibia_foreground_is_focused() => Assert.True(FocusRule.IsFocused(1, 1, Mirrors.Contains));
    [Fact] public void Mirror_foreground_is_focused() => Assert.True(FocusRule.IsFocused(21, 1, Mirrors.Contains));
    [Fact] public void Other_window_such_as_main_window_is_not_focused() => Assert.False(FocusRule.IsFocused(99, 1, Mirrors.Contains));
    [Fact] public void Not_focused_when_tibia_not_found() => Assert.False(FocusRule.IsFocused(20, null, Mirrors.Contains));
    [Fact] public void No_foreground_is_not_focused() => Assert.False(FocusRule.IsFocused(0, 0, _ => false));
}
