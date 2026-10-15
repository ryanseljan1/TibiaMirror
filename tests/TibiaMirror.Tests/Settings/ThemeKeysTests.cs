using System.Windows;
using System.Windows.Media;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class ThemeKeysTests
{
    static readonly string[] NewKeys = { "ThemeCheckedBrush", "ThemeCheckedTextBrush", "ThemeSuccessBrush", "ThemeOnSuccessBrush" };

    static double Lum(Color c)
    {
        static double L(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * L(c.R) + 0.7152 * L(c.G) + 0.0722 * L(c.B);
    }
    static double Contrast(Color a, Color b) { var (x, y) = (Lum(a), Lum(b)); return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05); }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Checked_and_success_pairs_are_defined_and_readable(bool dark)
    {
        var r = new ResourceDictionary();
        ThemeManager.Apply(r, dark);
        foreach (var k in NewKeys) Assert.IsType<SolidColorBrush>(r[k]);
        Color C(string k) => ((SolidColorBrush)r[k]).Color;
        Assert.True(Contrast(C("ThemeCheckedBrush"), C("ThemeCheckedTextBrush")) >= 4.5);
        Assert.True(Contrast(C("ThemeSuccessBrush"), C("ThemeOnSuccessBrush")) >= 4.5);
        Assert.True(Contrast(C("ThemeCheckedBrush"), C("ThemeControlBrush")) >= 1.3);
    }
}
