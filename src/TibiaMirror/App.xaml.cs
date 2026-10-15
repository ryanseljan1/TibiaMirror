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
        try { _host.Start(); }
        catch (Exception ex) { Log.Error("Startup failed", ex); }   // still show the window so the user is not left with a tray-only app
        finally
        {
            // Launched by the Windows Run key: stay in the tray; the tray icon or a second launch shows the window.
            if (!Settings.StartupRegistration.IsStartupLaunch(e.Args)) ShowMain();
        }
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

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        ExitApp();   // flushes autosave; the main window must not veto the shutdown
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DisposeHost();
        base.OnExit(e);
    }

    bool _hostDisposed;
    void DisposeHost()
    {
        if (_hostDisposed) return;
        _hostDisposed = true;
        try { _host?.Dispose(); }
        catch (Exception ex) { Log.Error("Host dispose failed", ex); }
    }

    void ExitApp()
    {
        if (_main is not null) _main.AllowClose = true;
        DisposeHost();
        _tray?.Dispose();
        _single?.Dispose();
        _single = null;
        Shutdown();
    }
}
