using System.Text.Json;
using TibiaMirror.Models;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class ThemeMigrationTests
{
    static ThemeMode? Migrate(string json)
    {
        var s = JsonSerializer.Deserialize<AppSettings>(json)!;
        AppSettingsNormalizer.Normalize(s);
        return s.ThemeMode;
    }

    [Fact] public void Legacy_dark_mode_true_becomes_dark() => Assert.Equal(ThemeMode.Dark, Migrate("{\"DarkMode\":true}"));
    [Fact] public void Legacy_dark_mode_false_becomes_light() => Assert.Equal(ThemeMode.Light, Migrate("{\"DarkMode\":false}"));
    [Fact] public void Fresh_settings_default_to_dark() => Assert.Equal(ThemeMode.Dark, Migrate("{}"));
    [Fact] public void Explicit_theme_mode_is_kept() => Assert.Equal(ThemeMode.System, Migrate("{\"DarkMode\":true,\"ThemeMode\":0}"));
}
