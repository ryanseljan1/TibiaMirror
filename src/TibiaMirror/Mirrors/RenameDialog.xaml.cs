using System.Windows;

namespace TibiaMirror.Mirrors;

public partial class RenameDialog : Window
{
    public string? Result { get; private set; }

    public RenameDialog(string current)
    {
        InitializeComponent();
        NameBox.Text = current;
        Validate();
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    void NameBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => Validate();

    void Validate()
    {
        if (OkButton is null || ErrorText is null) return;
        bool ok = NameBox.Text.Trim().Length is >= 1 and <= 40;
        OkButton.IsEnabled = ok;
        ErrorText.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
    }

    void Ok_Click(object sender, RoutedEventArgs e)
    {
        var text = NameBox.Text.Trim();
        if (text.Length is < 1 or > 40) return;
        Result = text;
        DialogResult = true;
    }
}
