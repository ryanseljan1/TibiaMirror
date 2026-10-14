using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TibiaMirror.Logging;

namespace TibiaMirror.Settings;

public partial class SettingsWindow : Window
{
    readonly AppHost _host;

    public SettingsWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();
        var s = host.Settings;
        ShowHideBox.Hotkey = s.ShowHideAllHotkey;
        NextProfileBox.Hotkey = s.NextProfileHotkey;
        OnlyFocusedCheck.IsChecked = s.OnlyShowWhenTibiaFocused;
        ProcessBox.Text = s.TibiaProcessName;
        TitleBox.Text = s.TibiaTitlePrefix;
        VersionText.Text = "Version " + AppInfo.Version;
        ProcessBox.TextChanged += (_, _) => Validate();
        TitleBox.TextChanged += (_, _) => Validate();
        OpenFolderButton.Click += (_, _) => OpenDataFolder();
        SaveButton.Click += (_, _) => Save();
        Validate();
    }

    /// <summary>Process name and title prefix must be non-empty.</summary>
    public static string? ValidateDetection(string? processName, string? titlePrefix) =>
        string.IsNullOrWhiteSpace(processName) ? "The Tibia process name can't be empty."
        : string.IsNullOrWhiteSpace(titlePrefix) ? "The Tibia window title prefix can't be empty."
        : null;

    void Validate()
    {
        var error = ValidateDetection(ProcessBox.Text, TitleBox.Text);
        ErrorText.Text = error ?? "";
        ErrorText.Visibility = error is null ? Visibility.Collapsed : Visibility.Visible;
        SaveButton.IsEnabled = error is null;
    }

    void OpenDataFolder()
    {
        try { Process.Start(new ProcessStartInfo("explorer.exe", "\"" + _host.Paths.Root + "\"") { UseShellExecute = true }); }
        catch (Exception e) { Log.Error("Open data folder failed", e); }
    }

    void Save()
    {
        if (ValidateDetection(ProcessBox.Text, TitleBox.Text) is not null) return;
        var s = _host.Settings;
        s.ShowHideAllHotkey = ShowHideBox.Hotkey;
        s.NextProfileHotkey = NextProfileBox.Hotkey;
        s.OnlyShowWhenTibiaFocused = OnlyFocusedCheck.IsChecked == true;
        s.TibiaProcessName = ProcessBox.Text.Trim();
        s.TibiaTitlePrefix = TitleBox.Text.Trim();
        try { _host.SettingsService?.Save(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error("Saving settings failed", e);
            MessageBox.Show(this, "Your settings were applied but could not be saved to disk: " + e.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        _host.ApplySettings();
        DialogResult = true;
    }
}
