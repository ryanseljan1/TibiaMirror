using System.Windows;
using System.Windows.Media;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class ThemeManagerTests
{
    [Fact]
    public void Applying_dark_then_light_updates_shared_window_and_text_brushes()
    {
        var resources = new ResourceDictionary();

        ThemeManager.Apply(resources, true);
        var darkBackground = ((SolidColorBrush)resources[ThemeManager.WindowBrushKey]).Color;
        var darkText = ((SolidColorBrush)resources[ThemeManager.TextBrushKey]).Color;
        Assert.True(darkBackground.R < darkText.R);

        ThemeManager.Apply(resources, false);
        var lightBackground = ((SolidColorBrush)resources[ThemeManager.WindowBrushKey]).Color;
        var lightText = ((SolidColorBrush)resources[ThemeManager.TextBrushKey]).Color;
        Assert.True(lightBackground.R > lightText.R);
        Assert.NotEqual(darkBackground, lightBackground);
    }
}
