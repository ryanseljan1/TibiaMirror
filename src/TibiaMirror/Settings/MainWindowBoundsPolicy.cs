using TibiaMirror.Models;

namespace TibiaMirror.Settings;

/// <summary>Pure decisions for restoring the main window position (physical px) at startup.</summary>
public static class MainWindowBoundsPolicy
{
    /// <summary>The rectangle to restore (saved bounds clamped onto the monitors), or null when nothing usable was saved.</summary>
    public static PixelRect? Target(PixelRect? saved, Func<PixelRect, PixelRect> clamp) =>
        saved is { IsEmpty: false } s ? clamp(s) : null;

    /// <summary>True when the window is not where it should be and the restore must be (re)applied.</summary>
    public static bool NeedsApply(PixelRect target, PixelRect actual) => target != actual;

    /// <summary>Bounds may only be saved once the startup restore has finished; WPF's own initial placement must not overwrite the saved ones.</summary>
    public static bool ShouldSave(bool restoreComplete, PixelRect current, PixelRect? saved) =>
        restoreComplete && !current.IsEmpty && current != saved;
}
