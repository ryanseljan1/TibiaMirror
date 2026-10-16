namespace TibiaMirror.Tests.Localization;

static class RepoPaths
{
    public static string Root
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TibiaMirror.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("TibiaMirror.sln not found");
        }
    }

    public static string Src => Path.Combine(Root, "src");
    public static string ResourcesDir => Path.Combine(Root, "src", "TibiaMirror", "Resources");

    public static IEnumerable<string> XamlFiles() =>
        Directory.EnumerateFiles(Src, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    public static Dictionary<string, HashSet<string>> ResxKeys() =>
        Directory.EnumerateFiles(ResourcesDir, "Strings.*.resx")
            .ToDictionary(f => Path.GetFileNameWithoutExtension(f).Split('.')[1],
                f => System.Xml.Linq.XDocument.Load(f).Root!.Elements("data")
                    .Select(d => (string)d.Attribute("name")!).ToHashSet());
}
