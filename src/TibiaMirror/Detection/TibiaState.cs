using TibiaMirror.Abstractions;

namespace TibiaMirror.Detection;

public sealed record TibiaState(WindowInfo? Window, bool IsFocused)
{
    public bool IsFound => Window is not null;
    public bool IsMinimized => Window?.IsMinimized ?? false;
}
