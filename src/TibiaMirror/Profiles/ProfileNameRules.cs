using TibiaMirror.Localization;

namespace TibiaMirror.Profiles;

public static class ProfileNameRules
{
    public const int MaxLength = 40;

    static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Returns a user-facing error message, or null when the name is acceptable.</summary>
    public static string? Validate(string name, IEnumerable<string> existing, string? renamingFrom = null)
    {
        var n = (name ?? "").Trim();
        if (n.Length == 0) return Loc.Get("Profiles_ErrEmpty");
        if (n.Length > MaxLength) return Loc.Format("Profiles_ErrTooLong", MaxLength);
        var bad = n.IndexOfAny(Path.GetInvalidFileNameChars());
        if (bad >= 0) return Loc.Format("Profiles_ErrBadChar", n[bad]);
        var dot = n.IndexOf('.');
        var stem = (dot >= 0 ? n[..dot] : n).TrimEnd();
        if (Reserved.Contains(stem)) return Loc.Format("Profiles_ErrReserved", stem.ToUpperInvariant());
        if (n.EndsWith('.')) return Loc.Get("Profiles_ErrEndsDot");
        foreach (var e in existing)
        {
            if (string.Equals(e, n, StringComparison.OrdinalIgnoreCase)
                && !(renamingFrom != null && string.Equals(e, renamingFrom, StringComparison.OrdinalIgnoreCase)))
                return Loc.Format("Profiles_ErrExists", e);
        }
        return null;
    }
}
