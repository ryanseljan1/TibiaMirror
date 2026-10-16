using System.Text.Json;

namespace TibiaMirror.Updates;

public sealed record ReleaseInfo(SemVersion Version, string Tag, string HtmlUrl, string Body, string InstallerName, string InstallerUrl, string? SumsUrl)
{
    public static ReleaseInfo? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;
            var tag = Str(root, "tag_name");
            var version = SemVersion.TryParse(tag);
            if (tag is null || version is null) return null;
            var assets = new List<(string Name, string Url)>();
            if (root.TryGetProperty("assets", out var arr) && arr.ValueKind == JsonValueKind.Array)
                foreach (var a in arr.EnumerateArray())
                {
                    var n = Str(a, "name");
                    var u = Str(a, "browser_download_url");
                    if (n is not null && u is not null) assets.Add((n, u));
                }
            var exact = $"TibiaMirror-Setup-{version}.exe";
            var installer = assets.FirstOrDefault(a => a.Name.Equals(exact, StringComparison.OrdinalIgnoreCase));
            if (installer.Name is null)
                installer = assets.FirstOrDefault(a => a.Name.StartsWith("TibiaMirror-Setup", StringComparison.OrdinalIgnoreCase) && a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (installer.Name is null) return null;
            var sums = assets.FirstOrDefault(a => a.Name.Equals("SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
            return new ReleaseInfo(version, tag, Str(root, "html_url") ?? "", Str(root, "body") ?? "", installer.Name, installer.Url, sums.Name is null ? null : sums.Url);
        }
        catch (JsonException) { return null; }
    }

    static string? Str(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
}
