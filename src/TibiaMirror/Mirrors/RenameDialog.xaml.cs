using System.Windows;

namespace TibiaMirror.Mirrors;

public partial class RenameDialog : Window
{
    public string? Result { get; private set; }

    public RenameDialog(string current)
    {
        InitializeComponent();
        NameBox.Text = current;
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        var text = NameBox.Text.Trim();
        if (text.Length is < 1 or > 40) return;
        Result = text;
        DialogResult = true;
    }
}
