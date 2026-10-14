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
        TimersView.Attach(host);
        if (host.Locator is { } locator) ShowTibiaState(locator.State);
        host.TibiaStateChanged += s => Dispatcher.Invoke(() => ShowTibiaState(s));
    }

    void ShowTibiaState(TibiaMirror.Detection.TibiaState s) => StatusText.Text =
        !s.IsFound ? "⚪ Waiting for Tibia…" : s.IsMinimized ? "🟡 Tibia minimized" : "🟢 Tibia detected";

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        if (!_host.Settings.TrayHintShown)
        {
            _tray.ShowBalloon(AppInfo.Name, "Still running in the tray. Right-click the tray icon to exit.");
            _host.Settings.TrayHintShown = true;
        }
    }
}
