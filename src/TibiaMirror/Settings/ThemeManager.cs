using System.Windows;
using System.Windows.Media;

namespace TibiaMirror.Settings;

/// <summary>Updates shared brushes so open WPF windows respond to a saved theme change.</summary>
public static class ThemeManager
{
    public const string WindowBrushKey = "ThemeWindowBrush";
    public const string TextBrushKey = "ThemeTextBrush";

    public static void Apply(bool darkMode) => Apply(Application.Current.Resources, darkMode);

    public static void Apply(ResourceDictionary resources, bool darkMode)
    {
        var colors = darkMode
            ? new[] { "#FF181B20", "#FFF2F4F7", "#FF22262D", "#FF303640", "#FFB4BBC7", "#FF515B69", "#FFFF8A8A", "#FF394B63", "#FF303640", "#FF59451D", "#FFFFE1A3" }
            : new[] { "#FFF7F8FA", "#FF1D2530", "#FFFFFFFF", "#FFF4F5F7", "#FF5D6672", "#FFACB4BE", "#FFB00020", "#FFDCEAFF", "#FFE8EBF0", "#FFFFF3CD", "#FF664D03" };
        string[] keys =
        {
            WindowBrushKey, TextBrushKey,
            "ThemeSurfaceBrush", "ThemeControlBrush", "ThemeMutedBrush", "ThemeBorderBrush",
            "ThemeErrorBrush", "ThemeSelectionBrush", "ThemeTabBrush", "ThemeBannerBrush", "ThemeBannerTextBrush"
        };
        // Keep every themed property bound through DynamicResource; replacing a brush updates open windows too.
        for (var i = 0; i < keys.Length; i++)
            resources[keys[i]] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i])!);
    }
}
