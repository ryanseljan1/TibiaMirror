using System.Windows;
using System.Windows.Media;
using TibiaMirror.Models;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class ThemeContrastTests
{
    static Color Over(Color fg, Color bg)
    {
        var a = fg.A / 255.0;
        byte M(byte f, byte b) => (byte)Math.Round(f * a + b * (1 - a));
        return Color.FromRgb(M(fg.R, bg.R), M(fg.G, bg.G), M(fg.B, bg.B));
    }

    [Theory]
    [InlineData(ThemeMode.Dark)]
    [InlineData(ThemeMode.Light)]
    public void Text_and_status_colours_meet_wcag_on_backgrounds(ThemeMode mode)
    {
        Sta.Run(() =>
        {
            var r = new ResourceDictionary();
            ThemeManager.Apply(r, mode, false, false);
            Color C(string k) => ThemeManagerTests.Brush(r, "Brush." + k);
            var fails = new List<string>();
            foreach (var bgName in new[] { "Window", "Surface", "SurfaceRaised" })
            {
                var bg = C(bgName);
                foreach (var fg in new[] { "Text", "TextSecondary", "TextMuted", "AccentText", "Danger", "Success", "Warning" })
                {
                    var ratio = ThemeKeysTests.Contrast(Over(C(fg), bg), bg);
                    if (ratio < 4.5) fails.Add($"{fg} on {bgName} {ratio:F2}");
                }
            }
            foreach (var bgName in new[] { "Window", "Surface" })
                foreach (var fg in new[] { "BorderControl", "Focus" })
                {
                    var ratio = ThemeKeysTests.Contrast(Over(C(fg), C(bgName)), C(bgName));
                    if (ratio < 3) fails.Add($"{fg} on {bgName} {ratio:F2}");
                }
            Assert.True(fails.Count == 0, mode + ": " + string.Join("; ", fails));
        });
    }
}
