using TibiaMirror.Models;

namespace TibiaMirror.Settings;

public static class AppSettingsNormalizer
{
    public static void Normalize(AppSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.TibiaProcessName)) s.TibiaProcessName = "client";
        if (string.IsNullOrWhiteSpace(s.TibiaTitlePrefix)) s.TibiaTitlePrefix = "Tibia";
    }
}
