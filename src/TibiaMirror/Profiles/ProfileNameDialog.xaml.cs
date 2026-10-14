using System.Windows;
using System.Windows.Controls;

namespace TibiaMirror.Profiles;

public partial class ProfileNameDialog : Window
{
    readonly Func<string, IEnumerable<string>> _existing;
    readonly string? _renamingFrom;

    public ProfileNameDialog(string title, string initial, Func<string, IEnumerable<string>> existing, string? renamingFrom = null)
    {
        InitializeComponent();
        Title = title;
        _existing = existing;
        _renamingFrom = renamingFrom;
        NameBox.Text = initial;
        NameBox.SelectAll();
        Loaded += (_, _) => NameBox.Focus();
    }

    public string ResultName { get; private set; } = "";

    void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var err = ProfileNameRules.Validate(NameBox.Text, _existing(NameBox.Text), _renamingFrom);
        ErrorText.Text = err ?? "";
        OkButton.IsEnabled = err == null;
    }

    void Ok_Click(object sender, RoutedEventArgs e) { ResultName = NameBox.Text.Trim(); DialogResult = true; }

    /// <summary>Shows the dialog; returns the trimmed name, or null if cancelled.</summary>
    public static string? Ask(Window? owner, string title, string initial, IEnumerable<string> existing, string? renamingFrom = null)
    {
        var list = existing.ToList();
        var d = new ProfileNameDialog(title, initial, _ => list, renamingFrom) { Owner = owner };
        return d.ShowDialog() == true ? d.ResultName : null;
    }
}
