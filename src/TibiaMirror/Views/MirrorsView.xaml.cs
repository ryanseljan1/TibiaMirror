using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TibiaMirror.Mirrors;

namespace TibiaMirror.Views;

public partial class MirrorsView : UserControl
{
    public MirrorsView() { InitializeComponent(); }

    void Name_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is MirrorCardViewModel card)
            card.RenameCommand.Execute(null);
    }
}

