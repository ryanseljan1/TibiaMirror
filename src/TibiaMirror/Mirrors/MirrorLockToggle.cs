using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public static class MirrorLockToggle
{
    /// <summary>True when the next toggle should lock: at least one visible mirror is still unlocked. Otherwise it unlocks.</summary>
    public static bool ShouldLock(IEnumerable<RegionModel> regions) => regions.Any(r => r.IsVisible && !r.IsLocked);
}
