namespace TibiaMirror.Detection;

/// <summary>Pure "is Tibia focused" rule: the foreground window is Tibia itself or one of our mirror windows.</summary>
public static class FocusRule
{
    public static bool IsFocused(nint foreground, nint? tibiaHandle, Func<nint, bool> isMirrorWindow) =>
        tibiaHandle is { } t && t != 0 && foreground != 0 && (foreground == t || isMirrorWindow(foreground));
}
