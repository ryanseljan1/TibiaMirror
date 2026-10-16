using System.Windows;
using System.Windows.Media;
using TibiaMirror.Models;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class ThemeKeysTests
{
    static readonly string[] NewKeys = { "ThemeCheckedBrush", "ThemeCheckedTextBrush", "ThemeSuccessBrush", "ThemeOnSuccessBrush" };

    public static double Lum(Color c)
    {
        static double L(byte v) { var s = v / 255.0; return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4); }
        return 0.2126 * L(c.R) + 0.7152 * L(c.G) + 0.0722 * L(c.B);
    }
    public static double Contrast(Color a, Color b) { var (x, y) = (Lum(a), Lum(b)); return (Math.Max(x, y) + 0.05) / (Math.Min(x, y) + 0.05); }

    public static HashSet<string> Keys(ThemeMode mode, bool hc)
    {
        var r = new ResourceDictionary();
        ThemeManager.Apply(r, mode, false, hc);
        return r.MergedDictionaries[0].Keys.Cast<object>().Select(k => k.ToString()!).ToHashSet();
    }

    [Theory]
    [InlineData(ThemeMode.Dark)]
    [InlineData(ThemeMode.Light)]
    public void Checked_and_success_pairs_are_defined_and_readable(ThemeMode mode)
    {
        Sta.Run(() =>
        {
            var r = new ResourceDictionary();
            ThemeManager.Apply(r, mode, false, false);
            foreach (var k in NewKeys) Assert.IsType<SolidColorBrush>(r.MergedDictionaries[0][k]);
            Color C(string k) => ThemeManagerTests.Brush(r, k);
            Assert.True(Contrast(C("ThemeCheckedBrush"), C("ThemeCheckedTextBrush")) >= 4.5);
            Assert.True(Contrast(C("ThemeSuccessBrush"), C("ThemeOnSuccessBrush")) >= 4.5);
            Assert.True(Contrast(C("ThemeCheckedBrush"), C("ThemeControlBrush")) >= 1.3);
        });
    }

    [Fact]
    public void Dark_light_and_high_contrast_define_the_same_keys()
    {
        Sta.Run(() =>
        {
            var dark = Keys(ThemeMode.Dark, false);
            Assert.True(dark.SetEquals(Keys(ThemeMode.Light, false)));
            Assert.True(dark.SetEquals(Keys(ThemeMode.Dark, true)));
        });
    }
}
