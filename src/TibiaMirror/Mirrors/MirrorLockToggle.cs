using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public static class MirrorLockToggle
{
    /// <summary>True when the next toggle should lock: at least one visible mirror is still unlocked. Otherwise it unlocks.</summary>
    public static bool ShouldLock(IEnumerable<RegionModel> regions) => regions.Any(r => r.IsVisible && !r.IsLocked);

    /// <summary>Resx key of the Lock all button label for the state the next toggle will produce.</summary>
    public static string LabelKey(bool willLock) => willLock ? "Main_LockAll" : "Main_UnlockAll";
}
