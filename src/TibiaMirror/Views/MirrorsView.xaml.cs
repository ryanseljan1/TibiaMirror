using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TibiaMirror.Mirrors;

namespace TibiaMirror.Views;

public partial class MirrorsView : UserControl
{
    public MirrorsView() { InitializeComponent(); }

    /// <summary>The glow chip opens the same On/Off, colour and intensity items as the mirror's context menu.</summary>
    void Glow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: MirrorCardViewModel card } button) return;
        var menu = new ContextMenu { PlacementTarget = button, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom };
        foreach (var item in GlowMenu.Items(card.Glow, card.SetGlow)) menu.Items.Add(item);
        menu.IsOpen = true;
    }

    void Name_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is MirrorCardViewModel card)
            card.RenameCommand.Execute(null);
    }
}

