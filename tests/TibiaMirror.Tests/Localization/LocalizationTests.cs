using System.Globalization;
using System.Text.RegularExpressions;
using TibiaMirror.Localization;

namespace TibiaMirror.Tests.Localization;

public class LocalizationTests
{
    static readonly string[] Areas = { "Common", "Main", "Mirrors", "Timers", "Settings", "Profiles", "Updates" };

    [Fact]
    public void Every_area_resx_exists_and_is_well_formed()
    {
        var keys = RepoPaths.ResxKeys();
        foreach (var a in Areas) Assert.True(keys.ContainsKey(a), $"missing Strings.{a}.resx");
    }

    [Fact]
    public void Resx_keys_carry_their_area_prefix()
    {
        foreach (var (area, keys) in RepoPaths.ResxKeys())
            foreach (var k in keys)
                Assert.True(k.StartsWith(area + "_", StringComparison.Ordinal), $"{k} in Strings.{area}.resx");
    }

    [Fact]
    public void Manifest_resource_names_resolve()
    {
        foreach (var a in Areas)
        {
            var rm = new System.Resources.ResourceManager(LocalizationManager.BaseNameFor(a), typeof(LocalizationManager).Assembly);
            Assert.NotNull(rm.GetResourceSet(CultureInfo.InvariantCulture, true, false));
        }
    }

    [Fact]
    public void Manager_returns_values_and_falls_back_to_key()
    {
        Assert.Equal("OK", Loc.Get("Common_Ok"));
        Assert.Equal("Nope_Missing", Loc.Get("Nope_Missing"));
        Assert.Equal("NoUnderscore", Loc.Get("NoUnderscore"));
    }

    [Fact]
    public void SetCulture_raises_indexer_change()
    {
        var raised = new List<string?>();
        var m = LocalizationManager.Instance;
        m.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        var old = m.Culture;
        m.SetCulture(old);
        Assert.Contains("Item[]", raised);
    }

    [Fact]
    public void Loc_keys_used_in_xaml_exist_in_matching_resx()
    {
        var keys = RepoPaths.ResxKeys();
        var rx = new Regex(@"\{(?:\w+:)?Loc\s+(?:Key=)?([A-Za-z0-9_]+)\s*\}");
        foreach (var f in RepoPaths.XamlFiles())
            foreach (Match m in rx.Matches(File.ReadAllText(f)))
            {
                var k = m.Groups[1].Value;
                var area = k.Split('_')[0];
                Assert.True(keys.TryGetValue(area, out var set) && set.Contains(k), $"{Path.GetFileName(f)}: {k}");
            }
    }

    [Fact]
    public void No_hardcoded_literals_in_xaml()
    {
        var rx = new Regex(@"\b(Content|Text|Header|Title|ToolTip|AutomationProperties\.Name)=""([^""]*)""");
        var failures = new List<string>();
        foreach (var f in RepoPaths.XamlFiles())
        {
            if (f.Contains($"{Path.DirectorySeparatorChar}Diagnostics{Path.DirectorySeparatorChar}")) continue;
            foreach (Match m in rx.Matches(File.ReadAllText(f)))
            {
                var v = m.Groups[2].Value;
                if (v.Length == 0 || v.StartsWith("{") || !v.Any(char.IsLetter)) continue;
                failures.Add($"{Path.GetFileName(f)}: {m.Groups[1].Value}=\"{v}\"");
            }
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void No_emoji_or_symbol_characters_outside_icons()
    {
        var sep = Path.DirectorySeparatorChar;
        var failures = new List<string>();
        var files = Directory.EnumerateFiles(RepoPaths.Src, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".xaml") || f.EndsWith(".cs"))
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}Diagnostics{sep}")
                        && !f.EndsWith($"Themes{sep}Icons.xaml"));
        foreach (var f in files)
            foreach (var line in File.ReadLines(f).Select((t, i) => (t, i)))
                for (var i = 0; i < line.t.Length; i++)
                {
                    var cp = char.ConvertToUtf32(line.t, i);
                    if (char.IsHighSurrogate(line.t[i])) i++;
                    if ((cp >= 0x2190 && cp <= 0x2BFF) || (cp >= 0x1F300 && cp <= 0x1FAFF))
                        failures.Add($"{Path.GetFileName(f)}:{line.i + 1} U+{cp:X4}");
                }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
