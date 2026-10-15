using System.ComponentModel;
using System.Windows;
using TibiaMirror.Shell;

namespace TibiaMirror.Views;

public partial class MainWindow : Window
{
    readonly AppHost _host;
    readonly TrayIcon _tray;

    public MainWindow(AppHost host, TrayIcon tray)
    {
        _host = host;
        _tray = tray;
        InitializeComponent();
        ProfilesTab.Init(host);
        host.ProfileChanged += p => Dispatcher.Invoke(() => ProfileText.Text = "Profile: " + p.Name);
        TimersView.Attach(host);
        MirrorsPage.DataContext = host.MirrorsVm;
        SnackbarHost.DataContext = host.Snackbar;
        ShowHideAllButton.Click += (_, _) => host.RequestToggleMirrors();
        LockAllButton.Click += (_, _) => host.Mirrors?.LockAll();
        SettingsButton.Click += (_, _) => new TibiaMirror.Settings.SettingsWindow(host) { Owner = this }.ShowDialog();
        RetryHotkeysButton.Click += (_, _) =>
        {
            if (!host.RetryHotkeys()) MessageBox.Show(this, "Hotkeys are still unavailable. Details were saved to the log folder.", AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        host.HotkeysUnavailableChanged += () => Dispatcher.Invoke(UpdateBanner);
        SourceInitialized += (_, _) => RestoreSavedBounds();
        LocationChanged += (_, _) => SaveBounds();
        SizeChanged += (_, _) => SaveBounds();
        if (host.Locator is { } locator) ShowTibiaState(locator.State);
        host.TibiaStateChanged += s => Dispatcher.Invoke(() => ShowTibiaState(s));
    }

    void ShowTibiaState(TibiaMirror.Detection.TibiaState s) => StatusText.Text =
        !s.IsFound ? "⚪ Waiting for Tibia…" : s.IsMinimized ? "🟡 Tibia minimized" : "🟢 Tibia detected";

    /// <summary>Set by App when the process is really exiting (tray Exit, session end) so Closing is not turned into hide-to-tray.</summary>
    public bool AllowClose { get; set; }

    void UpdateBanner() => HotkeyBanner.Visibility = _host.HotkeysUnavailable ? Visibility.Visible : Visibility.Collapsed;

    void RestoreSavedBounds()
    {
        if (_host.Settings.MainWindowBounds is not { } saved || saved.IsEmpty) return;
        var r = _host.ClampToMonitors(saved);
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        TibiaMirror.Interop.NativeMethods.SetWindowPos(hwnd, 0, r.X, r.Y, r.Width, r.Height, 0x0004 | TibiaMirror.Interop.NativeMethods.SWP_NOACTIVATE);
    }

    bool CaptureBounds()
    {
        if (WindowState != WindowState.Normal || !IsVisible) return false;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == 0 || !TibiaMirror.Interop.NativeMethods.GetWindowRect(hwnd, out var rc)) return false;
        var r = new TibiaMirror.Models.PixelRect(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);
        if (r.IsEmpty || r == _host.Settings.MainWindowBounds) return false;
        _host.Settings.MainWindowBounds = r;
        return true;
    }

    void SaveBounds() { if (CaptureBounds()) _host.ProfileEdited(); }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowClose) { base.OnClosing(e); return; }
        SaveBounds();
        e.Cancel = true;
        Hide();
        if (!_host.Settings.TrayHintShown)
        {
            _tray.ShowBalloon(AppInfo.Name, "Still running in the tray. Right-click the tray icon to exit.");
            _host.Settings.TrayHintShown = true;
            _host.ProfileEdited();
        }
    }
}
