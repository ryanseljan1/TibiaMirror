using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TibiaMirror.Models;
using TibiaMirror.Settings;
using TibiaMirror.Tests.Localization;

namespace TibiaMirror.Tests.Settings;

[Collection(WpfCollection.Name)]
public class ControlStylesSmokeTests
{
    /// <summary>
    /// A StaticResource to a key in a sibling dictionary (e.g. FocusVisual in Chrome.xaml) resolves to UnsetValue and only
    /// blows up when the property is used (FocusVisualStyle on the first Tab): no style setter may hold UnsetValue.
    /// </summary>
    [Fact]
    public void No_style_setter_is_unresolved() => Sta.Run(() =>
    {
        var resources = new ResourceDictionary();
        foreach (var name in new[] { "Tokens", "Light", "Icons", "Controls/Controls" })
            resources.MergedDictionaries.Add(new ResourceDictionary
                { Source = new Uri($"pack://application:,,,/TibiaMirror;component/Themes/{name}.xaml", UriKind.Absolute) });
        var bad = new List<string>();
        void Scan(ResourceDictionary d)
        {
            foreach (var key in d.Keys)
                for (var style = d[key] as Style; style is not null; style = style.BasedOn)
                    bad.AddRange(style.Setters.OfType<Setter>().Where(s => s.Value == DependencyProperty.UnsetValue)
                        .Select(s => $"{key}: {s.Property.Name}"));
            foreach (var m in d.MergedDictionaries) Scan(m);
        }
        Scan(resources);
        Assert.True(bad.Count == 0, string.Join("\n", bad.Distinct()));
    });

    /// <summary>StaticResource lookups inside templates only fail when the template is applied: instantiate every style.</summary>
    [Fact]
    public void Every_control_style_applies_in_every_theme() => Sta.Run(() =>
    {
        // Not `new App()`: only one Application may exist per process and SelectedTabThemeTests owns it.
        var app = new { Resources = new ResourceDictionary() };
        foreach (var name in new[] { "Tokens", "Light", "Icons", "Controls/Controls" })
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
                { Source = new Uri($"pack://application:,,,/TibiaMirror;component/Themes/{name}.xaml", UriKind.Absolute) });
        foreach (var hc in new[] { false, true })
        foreach (var mode in new[] { ThemeMode.Dark, ThemeMode.Light })
        {
            ThemeManager.Apply(app.Resources, mode, highContrast: hc);
            var panel = new StackPanel();
            foreach (var key in new[] { "Button.Primary", "Button.Secondary", "Button.Ghost", "Button.Danger", "Button.Icon" })
                panel.Children.Add(new Button { Content = "x", Style = (Style)app.Resources[key] });
            panel.Children.Add(new CheckBox { Content = "x", IsChecked = true, Style = (Style)app.Resources["ToggleSwitch"] });
            panel.Children.Add(new RadioButton { Content = "x", IsChecked = true, Style = (Style)app.Resources["Segmented"] });
            panel.Children.Add(new RadioButton { IsChecked = true, Style = (Style)app.Resources["Swatch"] });
            panel.Children.Add(new CheckBox { Content = "x" });
            panel.Children.Add(new RadioButton { Content = "x" });
            panel.Children.Add(new ToggleButton { Content = "x" });
            panel.Children.Add(new TextBox { Text = "x" });
            panel.Children.Add(new Slider { Value = 0.5, Maximum = 1 });
            panel.Children.Add(new ComboBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 });
            panel.Children.Add(new ListBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0 });
            panel.Children.Add(new ListView { ItemsSource = new[] { "a" } });
            panel.Children.Add(new Expander { Header = "x", Content = "y", IsExpanded = true });
            panel.Children.Add(new GroupBox { Header = "x", Content = "y" });
            var tabs = new TabControl();
            tabs.Items.Add(new TabItem { Header = "a", Content = "a" });
            panel.Children.Add(tabs);
            var menu = new ContextMenu();
            var parent = new MenuItem { Header = "parent" };
            parent.Items.Add(new MenuItem { Header = "child", IsCheckable = true, IsChecked = true });
            menu.Items.Add(parent);
            menu.Items.Add(new Separator());
            panel.ContextMenu = menu;
            menu.Resources = app.Resources;   // popups don't inherit window resources
            var tip = new ToolTip { Content = "tip" };
            var window = new Window
            {
                Style = (Style)app.Resources["AppWindowStyle"],
                Content = new ScrollViewer { Content = panel, Height = 200, VerticalScrollBarVisibility = ScrollBarVisibility.Visible },
                Width = 400, Height = 300, ShowActivated = false, Resources = app.Resources
            };
            tip.Resources = app.Resources;
            try
            {
                window.Show();
                window.UpdateLayout();
                menu.PlacementTarget = panel;
                menu.IsOpen = true;
                parent.IsSubmenuOpen = true;
                menu.UpdateLayout();
                tip.ApplyTemplate();
                menu.IsOpen = false;
            }
            finally { window.Close(); }
        }
    });

    /// <summary>A DynamicResource to a missing key fails silently: every key used under Themes/ must exist in each theme.</summary>
    [Fact]
    public void Every_dynamic_resource_under_Themes_is_defined()
    {
        var themes = Path.Combine(RepoPaths.Src, "TibiaMirror", "Themes");
        string[] shared = [File.ReadAllText(Path.Combine(themes, "Tokens.xaml")), File.ReadAllText(Path.Combine(themes, "Icons.xaml")),
            .. Directory.EnumerateFiles(Path.Combine(themes, "Controls"), "*.xaml").Select(File.ReadAllText)];
        var used = Directory.EnumerateFiles(themes, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"\{DynamicResource ([\w.]+)\}").Select(m => m.Groups[1].Value))
            .ToHashSet();
        var missing = new List<string>();
        foreach (var theme in new[] { "Dark", "Light", "HighContrast" })
        {
            var defined = shared.Append(File.ReadAllText(Path.Combine(themes, theme + ".xaml")))
                .SelectMany(t => Regex.Matches(t, @"x:Key=""([^""]+)""").Select(m => m.Groups[1].Value)).ToHashSet();
            missing.AddRange(used.Where(k => !defined.Contains(k)).Select(k => $"{theme}: {k}"));
        }
        Assert.True(missing.Count == 0, string.Join("\n", missing));
    }
}
