namespace TibiaMirror.Updates;

public static class UpdatePolicy
{
    public static bool ShouldOffer(SemVersion remote, SemVersion current, string? skipped)
    {
        if (!(remote > current)) return false;
        var s = SemVersion.TryParse(skipped);
        return s is null || !remote.Equals(s);
    }
}
