using TibiaMirror.Detection;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public static class MirrorVisibility
{
    public static bool ShouldShow(RegionModel r, TibiaState tibia, bool allHidden, bool onlyWhenFocused) =>
        r.IsVisible && !allHidden && tibia.IsFound && !tibia.IsMinimized && (!onlyWhenFocused || tibia.IsFocused);

    /// <summary>Locked mirrors always pass clicks through; unlocked ones too while Tibia is focused (if enabled), so clicks reach the game.
    /// Focusing any other window makes unlocked mirrors draggable again.</summary>
    public static bool IsClickThrough(RegionModel r, bool tibiaFocused, bool clickThroughWhenFocused) =>
        r.IsLocked || (clickThroughWhenFocused && tibiaFocused);
}
