using System.Windows.Controls;

namespace TibiaMirror.Controls;

/// <summary>Bottom-of-window "&lt;thing&gt; deleted · Undo" bar. DataContext is the <see cref="SnackbarService"/>.</summary>
public partial class Snackbar : UserControl
{
    public Snackbar() { InitializeComponent(); }
}
