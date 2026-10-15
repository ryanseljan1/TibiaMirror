using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

/// <summary>Builds the glow menu items shared by the mirror context menu and the card's glow chip, so both edit glow the same way.</summary>
public static class GlowMenu
{
    public static Brush BrushFor(string? hex)
    {
        GlowStyle.TryParse(GlowStyle.Normalize(hex), out var r, out var g, out var b);
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    /// <summary>On/Off, colour presets and intensity for <paramref name="current"/>; each pick calls <paramref name="apply"/> with the updated copy.</summary>
    public static IEnumerable<Control> Items(GlowSettings current, Action<GlowSettings> apply)
    {
        var colour = GlowStyle.Normalize(current.Color);
        yield return new MenuItem
        {
            Header = "Glow on", IsCheckable = true, IsChecked = current.Enabled,
            Command = new Relay(() => apply(current with { Enabled = !current.Enabled })),
        };
        yield return new Separator();
        foreach (var p in GlowStyle.Presets)
        {
            yield return new MenuItem
            {
                Header = p.Name, IsCheckable = true, IsChecked = p.Hex == colour,
                Icon = new Rectangle { Width = 12, Height = 12, Fill = BrushFor(p.Hex), Stroke = Brushes.Gray, StrokeThickness = 1 },
                Command = new Relay(() => apply(current with { Color = p.Hex })),
            };
        }
        yield return new Separator();
        foreach (var i in Enum.GetValues<GlowIntensity>())
        {
            yield return new MenuItem
            {
                Header = i.ToString(), IsCheckable = true, IsChecked = i == GlowStyle.Normalize(current.Intensity),
                Command = new Relay(() => apply(current with { Intensity = i })),
            };
        }
    }

    /// <summary>Minimal ICommand over an action (menu items are rebuilt on every open, so no CanExecute tracking is needed).</summary>
    sealed class Relay(Action run) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => run();
    }
}
