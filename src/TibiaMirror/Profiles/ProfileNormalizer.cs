using TibiaMirror.Audio;
using TibiaMirror.Models;

namespace TibiaMirror.Profiles;

/// <summary>Repairs hand-edited or imported profiles so the rest of the app can rely on non-null data.</summary>
public static class ProfileNormalizer
{
    public static void Normalize(Profile p)
    {
        p.Regions ??= new();
        p.Timers ??= new();
        p.Regions.RemoveAll(r => r is null);
        p.Timers.RemoveAll(t => t is null);
        foreach (var r in p.Regions)
        {
            r.Name ??= "Region";
            r.Countdown ??= new();
        }
        foreach (var t in p.Timers)
        {
            t.Name ??= "Timer";
            if (string.IsNullOrEmpty(t.Sound)) t.Sound = BuiltInSounds.DefaultId;
        }
    }
}
