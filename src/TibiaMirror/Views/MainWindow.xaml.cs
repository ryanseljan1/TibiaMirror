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
    }

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
