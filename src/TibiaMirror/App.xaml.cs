using System.Windows;
using TibiaMirror.Logging;
using TibiaMirror.Shell;
using TibiaMirror.Views;

namespace TibiaMirror;

public partial class App : Application
{
    SingleInstance? _single;
    AppHost? _host;
    TrayIcon? _tray;
    MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _single = new SingleInstance();
        if (!_single.IsFirst)
        {
            _single.SignalFirstInstance();
            _single.Dispose();
            _single = null;
            Shutdown();
            return;
        }

        var paths = AppPaths.Default();
        Log.Init(paths.LogsDir);
        Log.Info($"{AppInfo.Name} {AppInfo.Version} starting");

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error("Unhandled UI exception", args.Exception);
            ShowCrashMessage(paths);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Error("Unhandled exception", args.ExceptionObject as Exception);
            ShowCrashMessage(paths);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Error("Unobserved task exception", args.Exception);
            ShowCrashMessage(paths);
        };

        _host = new AppHost(paths);
        _tray = new TrayIcon();
        _main = new MainWindow(_host, _tray);
        _tray.ShowRequested += ShowMain;
        _tray.ToggleMirrorsRequested += _host.RequestToggleMirrors;
        _tray.ExitRequested += ExitApp;
        _single.ListenForShowRequests(() => Dispatcher.Invoke(ShowMain));
        _host.Start();
        ShowMain();
    }

    static void ShowCrashMessage(AppPaths paths) =>
        MessageBox.Show("Something went wrong. Details were saved to the log folder:\n" + paths.LogsDir, AppInfo.Name);

    void ShowMain()
    {
        if (_main is null) return;
        _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    void ExitApp()
    {
        _host?.Dispose();
        _tray?.Dispose();
        _single?.Dispose();
        _single = null;
        Shutdown();
    }
}
