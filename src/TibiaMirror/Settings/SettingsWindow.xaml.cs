using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Settings;

public partial class SettingsWindow : Window
{
    readonly AppHost _host;
    readonly StartupRegistration _startup = new(new RegistryRunKey(), Environment.ProcessPath ?? "");
    bool _startupWasEnabled;

    public SettingsWindow(AppHost host)
    {
        _host = host;
        InitializeComponent();
        var s = host.Settings;
        ShowHideBox.Hotkey = s.ShowHideAllHotkey;
        NextProfileBox.Hotkey = s.NextProfileHotkey;
        LockAllBox.Hotkey = s.LockAllHotkey;
        OnlyFocusedCheck.IsChecked = s.OnlyShowWhenTibiaFocused;
        ClickThroughCheck.IsChecked = s.ClickThroughWhileTibiaFocused;
        ThemeModeCombo.SelectedIndex = (int)(s.ThemeMode ?? ThemeMode.Dark);
        _startupWasEnabled = ReadStartupEnabled();
        StartWithWindowsCheck.IsChecked = _startupWasEnabled;
        ProcessBox.Text = s.TibiaProcessName;
        TitleBox.Text = s.TibiaTitlePrefix;
        VersionText.Text = "Version " + AppInfo.Version;
        ProcessBox.TextChanged += (_, _) => Validate();
        TitleBox.TextChanged += (_, _) => Validate();
        OpenFolderButton.Click += (_, _) => OpenDataFolder();
        SaveButton.Click += (_, _) => Save();
        Validate();
    }

    bool ReadStartupEnabled()
    {
        try { return _startup.IsEnabled(); }
        catch (Exception e) { Log.Error("Reading the startup entry failed", e); return false; }
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

    /// <summary>Warns (Yes/No) when a changed global hotkey is used by a timer, a countdown, or by another global hotkey.</summary>
    bool ConfirmHotkeyConflicts()
    {
        var s = _host.Settings;
        var showHide = ShowHideBox.Hotkey;
        var nextProfile = NextProfileBox.Hotkey;
        var lockAll = LockAllBox.Hotkey;
        var boxes = new (Hotkey? Key, bool Changed, object Owner)[]
        {
            (showHide, showHide != s.ShowHideAllHotkey, _host.ShowHideOwner),
            (nextProfile, nextProfile != s.NextProfileHotkey, _host.NextProfileOwner),
            (lockAll, lockAll != s.LockAllHotkey, _host.LockAllOwner),
        };
        foreach (var c in boxes)
        {
            if (c.Key is not { } hk || !c.Changed) continue;
            // The other global hotkeys are judged from the boxes (their new values), not from their current registrations.
            var siblings = boxes.Where(b => !ReferenceEquals(b.Owner, c.Owner)).ToList();
            var others = (_host.Hotkeys?.FindConflicts(hk, c.Owner) ?? Array.Empty<object>())
                .Where(o => !siblings.Any(b => ReferenceEquals(b.Owner, o))).ToList();
            others.AddRange(siblings.Where(b => b.Key == hk).Select(b => b.Owner));
            if (others.Count == 0) continue;
            var names = string.Join(", ", others.Select(o => o is Models.AudioTimerModel t ? t.Name : o.ToString()));
            if (MessageBox.Show(this, $"This key is already used by: {names}. Use it anyway?", "Key already in use",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return false;
        }
        return true;
    }

    /// <summary>Writes the Run entry only when the checkbox changed, so an installer-made entry is left alone.</summary>
    void ApplyStartup()
    {
        var want = StartWithWindowsCheck.IsChecked == true;
        if (want == _startupWasEnabled) return;
        try { _startup.Apply(want); }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            Log.Error("Updating the startup entry failed", e);
            MessageBox.Show(this, "Could not change the Start with Windows setting: " + e.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    void Save()
    {
        if (ValidateDetection(ProcessBox.Text, TitleBox.Text) is not null) return;
        if (!ConfirmHotkeyConflicts()) return;
        var s = _host.Settings;
        s.ShowHideAllHotkey = ShowHideBox.Hotkey;
        s.NextProfileHotkey = NextProfileBox.Hotkey;
        s.LockAllHotkey = LockAllBox.Hotkey;
        s.OnlyShowWhenTibiaFocused = OnlyFocusedCheck.IsChecked == true;
        s.ClickThroughWhileTibiaFocused = ClickThroughCheck.IsChecked == true;
        s.ThemeMode = (ThemeMode)Math.Max(0, ThemeModeCombo.SelectedIndex);
        s.TibiaProcessName = ProcessBox.Text.Trim();
        s.TibiaTitlePrefix = TitleBox.Text.Trim();
        try { _host.SettingsService?.Save(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error("Saving settings failed", e);
            MessageBox.Show(this, "Your settings were applied but could not be saved to disk: " + e.Message, AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        ApplyStartup();
        _host.ApplySettings();
        DialogResult = true;
    }
}
