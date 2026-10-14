using TibiaMirror.Geometry;
using TibiaMirror.Models;

namespace TibiaMirror.Settings;

public static class RestoreClamp
{
    public static void ClampProfile(Profile p, IReadOnlyList<PixelRect> workAreas, PixelRect primary)
    {
        foreach (var r in p.Regions) r.Mirror = MonitorClamp.Clamp(r.Mirror, workAreas, primary);
    }

    public static PixelRect Clamp(PixelRect rect, IReadOnlyList<PixelRect> workAreas, PixelRect primary) =>
        MonitorClamp.Clamp(rect, workAreas, primary);
}
