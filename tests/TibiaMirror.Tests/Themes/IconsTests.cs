using System.Xml.Linq;
using TibiaMirror.Tests.Localization;

namespace TibiaMirror.Tests.Themes;

public class IconsTests
{
    [Fact]
    public void Glyphs_are_single_private_use_chars()
    {
        var doc = XDocument.Load(Path.Combine(RepoPaths.Src, "TibiaMirror", "Themes", "Icons.xaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var glyphs = doc.Root!.Elements().Where(e => ((string?)e.Attribute(x + "Key"))?.StartsWith("Glyph.") == true).ToList();
        Assert.NotEmpty(glyphs);
        foreach (var g in glyphs)
        {
            var v = g.Value;
            Assert.True(v.Length == 1 && v[0] >= 0xE000 && v[0] <= 0xF8FF, (string)g.Attribute(x + "Key")!);
        }
    }
}
