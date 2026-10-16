using System.Globalization;

namespace TibiaMirror.Updates;

public sealed class SemVersion : IComparable<SemVersion>, IEquatable<SemVersion>
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public string Prerelease { get; }

    SemVersion(int major, int minor, int patch, string pre) { Major = major; Minor = minor; Patch = patch; Prerelease = pre; }

    public static SemVersion? TryParse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var s = text.Trim();
        if (s.StartsWith('v') || s.StartsWith('V')) s = s[1..];
        var plus = s.IndexOf('+');
        if (plus >= 0) s = s[..plus];
        var pre = "";
        var dash = s.IndexOf('-');
        if (dash >= 0) { pre = s[(dash + 1)..]; s = s[..dash]; }
        var parts = s.Split('.');
        if (parts.Length is < 1 or > 4) return null;
        var n = new int[3];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.None, CultureInfo.InvariantCulture, out var v)) return null;
            if (i < 3) n[i] = v;
        }
        return new SemVersion(n[0], n[1], n[2], pre);
    }

    public static SemVersion Parse(string text) => TryParse(text) ?? throw new FormatException($"Not a version: '{text}'");

    public int CompareTo(SemVersion? other)
    {
        if (other is null) return 1;
        var c = Major.CompareTo(other.Major);
        if (c != 0) return c;
        c = Minor.CompareTo(other.Minor);
        if (c != 0) return c;
        c = Patch.CompareTo(other.Patch);
        if (c != 0) return c;
        if (Prerelease.Length == 0 && other.Prerelease.Length == 0) return 0;
        if (Prerelease.Length == 0) return 1;
        if (other.Prerelease.Length == 0) return -1;
        return ComparePre(Prerelease, other.Prerelease);
    }

    static int ComparePre(string a, string b)
    {
        var x = a.Split('.');
        var y = b.Split('.');
        for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
        {
            var xn = int.TryParse(x[i], out var xi);
            var yn = int.TryParse(y[i], out var yi);
            int c;
            if (xn && yn) c = xi.CompareTo(yi);
            else if (xn) c = -1;
            else if (yn) c = 1;
            else c = string.CompareOrdinal(x[i], y[i]);
            if (c != 0) return c;
        }
        return x.Length.CompareTo(y.Length);
    }

    public bool Equals(SemVersion? other) => other is not null && CompareTo(other) == 0;
    public override bool Equals(object? obj) => Equals(obj as SemVersion);
    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Prerelease);
    public override string ToString() => $"{Major}.{Minor}.{Patch}" + (Prerelease.Length > 0 ? "-" + Prerelease : "");
    public static bool operator >(SemVersion a, SemVersion b) => a.CompareTo(b) > 0;
    public static bool operator <(SemVersion a, SemVersion b) => a.CompareTo(b) < 0;
}
