using TibiaMirror.Models;

namespace TibiaMirror.Settings;

public static class AppSettingsNormalizer
{
    public static void Normalize(AppSettings s)
    {
        s.ThemeMode ??= s.DarkMode is { } dark ? (dark ? ThemeMode.Dark : ThemeMode.Light) : ThemeMode.Dark;
        s.DarkMode = null;   // migrated; stop writing the legacy key
        if (string.IsNullOrWhiteSpace(s.TibiaProcessName)) s.TibiaProcessName = "client";
        if (string.IsNullOrWhiteSpace(s.TibiaTitlePrefix)) s.TibiaTitlePrefix = "Tibia";
    }
}
