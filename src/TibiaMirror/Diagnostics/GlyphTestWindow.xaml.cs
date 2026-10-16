using System.Windows;
using System.Windows.Controls;

namespace TibiaMirror.Diagnostics;

public partial class GlyphTestWindow : Window
{
    public GlyphTestWindow()
    {
        InitializeComponent();
        var icons = (ResourceDictionary)new ResourceDictionary { Source = new Uri("Themes/Icons.xaml", UriKind.Relative) };
        var glyphStyle = (Style)icons["Glyph"];
        foreach (var key in icons.Keys.OfType<string>().Where(k => k.StartsWith("Glyph.", StringComparison.Ordinal)).OrderBy(k => k))
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock { Text = (string)icons[key], Style = glyphStyle, Width = 36 });
            row.Children.Add(new TextBlock { Text = $"{key}  U+{(int)((string)icons[key])[0]:X4}", VerticalAlignment = VerticalAlignment.Center });
            List.Items.Add(row);
        }
    }
}
