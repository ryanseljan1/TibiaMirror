using System.ComponentModel;
using System.Windows;
using TibiaMirror.Localization;
using TibiaMirror.Mirrors;
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
        host.ProfileChanged += p => Dispatcher.Invoke(() => ProfileText.Text = Loc.Format("Main_Profile", p.Name));
        TimersView.Attach(host);
        MirrorsPage.DataContext = host.MirrorsVm;
        SnackbarHost.DataContext = host.Snackbar;
        ShowHideAllButton.Click += (_, _) => host.RequestToggleMirrors();
        LockAllButton.Click += (_, _) => host.Mirrors?.ToggleLockAll();   // same toggle as the hotkey
        host.MirrorsVm.LockAllStateChanged += () => Dispatcher.Invoke(UpdateLockAll);
        UpdateLockAll();
        SettingsButton.Click += (_, _) => new TibiaMirror.Settings.SettingsWindow(host) { Owner = this }.ShowDialog();
        RetryHotkeysButton.Click += (_, _) =>
        {
            if (!host.RetryHotkeys()) MessageBox.Show(this, Loc.Get("Main_HotkeysStillUnavailable"), AppInfo.Name, MessageBoxButton.OK, MessageBoxImage.Warning);
        };
        host.HotkeysUnavailableChanged += () => Dispatcher.Invoke(UpdateBanner);
        // WPF performs its own initial placement (default cascade position, DIP width/height) after SourceInitialized, and its
        // LocationChanged/SizeChanged then saved those defaults over the stored bounds. So: restore early (no flicker), re-apply once
        // the window is rendered, and only allow saving after that.
        SourceInitialized += (_, _) => RestoreSavedBounds();
        ContentRendered += (_, _) => { RestoreSavedBounds(); _restoreComplete = true; };
        LocationChanged += (_, _) => SaveBounds();
        SizeChanged += (_, _) => SaveBounds();
        if (host.Locator is { } locator) ShowTibiaState(locator.State);
        host.TibiaStateChanged += s => Dispatcher.Invoke(() => ShowTibiaState(s));
    }

    /// <summary>Re-reads the Lock all / Unlock all label from the mirrors' state (the click and the hotkey share ToggleLockAll).</summary>
    void UpdateLockAll()
    {
        var willLock = _host.MirrorsVm.LockAllWillLock;
        LockAllText.Text = Loc.Get(MirrorLockToggle.LabelKey(willLock));
        LockAllGlyph.Text = (string)FindResource(willLock ? "Glyph.Lock" : "Glyph.Unlock");
    }


    void ShowTibiaState(TibiaMirror.Detection.TibiaState s)
    {
        var (text, dotKey) = !s.IsFound ? (Loc.Get("Main_StatusWaiting"), "Brush.TextMuted") : s.IsMinimized ? (Loc.Get("Main_StatusMinimized"), "Brush.Warning") : (Loc.Get("Main_StatusDetected"), "Brush.Success");
        StatusText.Text = text;
        StatusDot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, dotKey);
        System.Windows.Automation.AutomationProperties.SetName(StatusText, text);
    }

    /// <summary>Set by App when the process is really exiting (tray Exit, session end) so Closing is not turned into hide-to-tray.</summary>
    public bool AllowClose { get; set; }

    void UpdateBanner() => HotkeyBanner.Visibility = _host.HotkeysUnavailable ? Visibility.Visible : Visibility.Collapsed;

    bool _restoreComplete;

    void RestoreSavedBounds()
    {
        if (TibiaMirror.Settings.MainWindowBoundsPolicy.Target(_host.Settings.MainWindowBounds, _host.ClampToMonitors) is not { } r) return;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == 0) return;
        if (TibiaMirror.Interop.NativeMethods.GetWindowRect(hwnd, out var rc)
            && !TibiaMirror.Settings.MainWindowBoundsPolicy.NeedsApply(r, new TibiaMirror.Models.PixelRect(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top))) return;
        TibiaMirror.Interop.NativeMethods.SetWindowPos(hwnd, 0, r.X, r.Y, r.Width, r.Height, 0x0004 | TibiaMirror.Interop.NativeMethods.SWP_NOACTIVATE);
    }

    bool CaptureBounds()
    {
        if (WindowState != WindowState.Normal || !IsVisible) return false;
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        if (hwnd == 0 || !TibiaMirror.Interop.NativeMethods.GetWindowRect(hwnd, out var rc)) return false;
        var r = new TibiaMirror.Models.PixelRect(rc.Left, rc.Top, rc.Right - rc.Left, rc.Bottom - rc.Top);
        if (!TibiaMirror.Settings.MainWindowBoundsPolicy.ShouldSave(_restoreComplete, r, _host.Settings.MainWindowBounds)) return false;
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
            _tray.ShowBalloon(AppInfo.Name, Loc.Get("Main_TrayHint"));
            _host.Settings.TrayHintShown = true;
            _host.ProfileEdited();
        }
    }
}
