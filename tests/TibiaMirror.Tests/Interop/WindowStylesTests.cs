using TibiaMirror.Interop;
namespace TibiaMirror.Tests.Interop;

public class WindowStylesTests
{
    const long Transparent = 0x20, ToolWindow = 0x80, Layered = 0x80000;

    [Fact] public void Enable_adds_transparent_and_layered_and_keeps_other_bits()
        => Assert.Equal(ToolWindow | Transparent | Layered, WindowStyles.ComputeExStyle(ToolWindow, true));

    [Fact] public void Disable_removes_only_transparent_and_keeps_layered()
        => Assert.Equal(ToolWindow | Layered, WindowStyles.ComputeExStyle(ToolWindow | Transparent | Layered, false));

    [Fact] public void Disable_without_layered_does_not_add_it()
        => Assert.Equal(ToolWindow, WindowStyles.ComputeExStyle(ToolWindow | Transparent, false));
}
