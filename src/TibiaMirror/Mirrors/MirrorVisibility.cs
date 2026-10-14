using TibiaMirror.Detection;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public static class MirrorVisibility
{
    public static bool ShouldShow(RegionModel r, TibiaState tibia, bool allHidden, bool onlyWhenFocused) =>
        r.IsVisible && !allHidden && tibia.IsFound && !tibia.IsMinimized && (!onlyWhenFocused || tibia.IsFocused);
}
