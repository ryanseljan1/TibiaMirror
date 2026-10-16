using System.Windows;
using Microsoft.Win32;
using TibiaMirror.Models;

namespace TibiaMirror.Settings;

/// <summary>Swaps the merged theme dictionary so open WPF windows follow the saved theme.</summary>
public static class ThemeManager
{
    public const string WindowBrushKey = "Brush.Window";
    public const string TextBrushKey = "Brush.Text";

    static ThemeMode _mode = ThemeMode.Dark;
    static bool _subscribed;

    public static event EventHandler? ThemeChanged;
    public static bool IsDark { get; private set; } = true;

    public static void Apply(ThemeMode mode)
    {
        _mode = mode;
        Subscribe();
        Apply(Application.Current.Resources, mode);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Apply(ResourceDictionary resources, ThemeMode mode, bool? systemLight = null, bool? highContrast = null)
    {
        var name = (highContrast ?? SystemParameters.HighContrast) ? "HighContrast"
            : mode == ThemeMode.Dark ? "Dark"
            : mode == ThemeMode.Light ? "Light"
            : (systemLight ?? SystemUsesLightTheme()) ? "Light" : "Dark";
        var theme = new ResourceDictionary { Source = new Uri($"pack://application:,,,/TibiaMirror;component/Themes/{name}.xaml", UriKind.Absolute) };
        var merged = resources.MergedDictionaries;
        var index = -1;
        for (var i = 0; i < merged.Count; i++)
            if (merged[i].Contains("ThemeId")) { index = i; break; }
        if (index >= 0) merged[index] = theme; else merged.Add(theme);
        IsDark = name != "Light";
    }

    static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is not int v || v != 0;
        }
        catch { return true; }
    }

    static void Subscribe()
    {
        if (_subscribed) return;
        _subscribed = true;
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (_mode == ThemeMode.System && e.Category == UserPreferenceCategory.General) Reapply();
        };
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.HighContrast)) Reapply();
        };
    }

    static void Reapply()
    {
        var app = Application.Current;
        app?.Dispatcher.BeginInvoke(() => Apply(_mode));
    }
}
