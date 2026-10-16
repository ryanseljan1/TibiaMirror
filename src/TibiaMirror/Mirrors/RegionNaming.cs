using TibiaMirror.Localization;

namespace TibiaMirror.Mirrors;

public static class RegionNaming
{
    /// <summary>"Region N" with the lowest N not already used (case-insensitive).</summary>
    public static string NextName(IEnumerable<string?> existing)
    {
        var used = new HashSet<string?>(existing, StringComparer.OrdinalIgnoreCase);
        var n = 1;
        while (used.Contains(Loc.Format("Mirrors_RegionName", n))) n++;
        return Loc.Format("Mirrors_RegionName", n);
    }
}
