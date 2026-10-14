using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TibiaMirror.Settings;

namespace TibiaMirror.Tests.Settings;

public class SelectedTabThemeTests
{
    [Fact]
    public void Selected_tab_does_not_render_a_white_background_in_dark_mode()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                ThemeManager.Apply(app.Resources, true);
                var tabs = new TabControl();
                foreach (var name in new[] { "Mirrors", "Audio Timers", "Profiles" })
                    tabs.Items.Add(new TabItem { Header = name, Content = new TextBlock { Text = name } });
                var window = new Window { Content = tabs, Width = 300, Height = 200 };
                try
                {
                    window.Show();
                    var surface = ((SolidColorBrush)app.Resources["ThemeSurfaceBrush"]).Color;
                    for (var i = 0; i < tabs.Items.Count; i++)
                    {
                        tabs.SelectedIndex = i;
                        window.UpdateLayout();
                        var selected = (TabItem)tabs.Items[i];
                        var colors = DescendantBorders(selected).Select(b => b.Background)
                            .OfType<SolidColorBrush>().Select(b => b.Color).ToList();
                        Assert.Contains(surface, colors);
                        Assert.DoesNotContain(colors, c => c.R > 240 && c.G > 240 && c.B > 240);
                    }
                    ThemeManager.Apply(app.Resources, false);
                    window.UpdateLayout();
                    var lightSurface = ((SolidColorBrush)app.Resources["ThemeSurfaceBrush"]).Color;
                    Assert.Contains(lightSurface, DescendantBorders((TabItem)tabs.SelectedItem)
                        .Select(b => b.Background).OfType<SolidColorBrush>().Select(b => b.Color));
                    ThemeManager.Apply(app.Resources, true);
                    window.UpdateLayout();
                    Assert.Contains(surface, DescendantBorders((TabItem)tabs.SelectedItem)
                        .Select(b => b.Background).OfType<SolidColorBrush>().Select(b => b.Color));
                }
                finally { window.Close(); app.Shutdown(); }
            }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "WPF tab rendering did not finish");
        if (failure is not null) throw failure;
    }

    static IEnumerable<Border> DescendantBorders(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Border border) yield return border;
            foreach (var nested in DescendantBorders(child)) yield return nested;
        }
    }
}
