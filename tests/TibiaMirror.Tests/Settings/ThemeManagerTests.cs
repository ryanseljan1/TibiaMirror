using System.Windows;
using System.Windows.Media;
using TibiaMirror.Models;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

[Collection(WpfCollection.Name)]
public class ThemeManagerTests
{
    public static Color Brush(ResourceDictionary r, string key)
    {
        foreach (var d in r.MergedDictionaries)
            if (d[key] is SolidColorBrush b) return b.Color;
        return ((SolidColorBrush)r[key]).Color;
    }

    [Fact]
    public void Applying_the_same_mode_twice_keeps_the_dictionary_instance()
    {
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            ThemeManager.Apply(resources, ThemeMode.Dark, highContrast: false);
            var first = resources.MergedDictionaries[0];
            ThemeManager.Apply(resources, ThemeMode.Dark, highContrast: false);
            Assert.Same(first, resources.MergedDictionaries[0]);
            ThemeManager.Apply(resources, ThemeMode.Light, highContrast: false);
            Assert.NotSame(first, resources.MergedDictionaries[0]);
        });
    }

    [Fact]
    public void Applying_dark_then_light_swaps_the_theme_dictionary_in_place()
    {
        Sta.Run(() =>
        {
            var resources = new ResourceDictionary();
            resources.MergedDictionaries.Add(new ResourceDictionary { ["Other"] = 1 });
            resources.MergedDictionaries.Add(new ResourceDictionary { ["ThemeId"] = "Old" });
            resources.MergedDictionaries.Add(new ResourceDictionary { ["Other2"] = 2 });

            ThemeManager.Apply(resources, ThemeMode.Dark, highContrast: false);
            Assert.Equal(3, resources.MergedDictionaries.Count);
            Assert.Equal("Dark", resources.MergedDictionaries[1]["ThemeId"]);
            var darkBackground = Brush(resources, ThemeManager.WindowBrushKey);
            var darkText = Brush(resources, ThemeManager.TextBrushKey);
            Assert.True(darkBackground.R < darkText.R);

            ThemeManager.Apply(resources, ThemeMode.Light, highContrast: false);
            Assert.Equal("Light", resources.MergedDictionaries[1]["ThemeId"]);
            Assert.True(Brush(resources, ThemeManager.WindowBrushKey).R > Brush(resources, ThemeManager.TextBrushKey).R);
        });
    }

    [Theory]
    [InlineData(true, "Light")]
    [InlineData(false, "Dark")]
    public void System_mode_follows_the_os_preference(bool systemLight, string expected)
    {
        Sta.Run(() =>
        {
            var r = new ResourceDictionary();
            ThemeManager.Apply(r, ThemeMode.System, systemLight, false);
            Assert.Equal(expected, r.MergedDictionaries[0]["ThemeId"]);
        });
    }

    [Theory]
    [InlineData(ThemeMode.Dark)]
    [InlineData(ThemeMode.Light)]
    [InlineData(ThemeMode.System)]
    public void High_contrast_wins_over_the_mode(ThemeMode mode)
    {
        Sta.Run(() =>
        {
            var r = new ResourceDictionary();
            ThemeManager.Apply(r, mode, true, true);
            Assert.Equal("HighContrast", r.MergedDictionaries[0]["ThemeId"]);
        });
    }
}

static class Sta
{
    /// <summary>pack://application URIs only resolve after Application's static constructor ran (it registers the pack
    /// web-request factory); without this, tests pass or fail depending on whether another test created an App first.</summary>
    static void EnsurePackUris()
    {
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(System.Windows.Application).TypeHandle);
    }

    public static void Run(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() => { try { EnsurePackUris(); action(); } catch (Exception ex) { failure = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "WPF test did not finish");
        if (failure is not null) throw failure;
    }
}
