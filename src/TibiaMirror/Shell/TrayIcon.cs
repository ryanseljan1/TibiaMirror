using WinForms = System.Windows.Forms;

namespace TibiaMirror.Shell;

public sealed class TrayIcon : IDisposable
{
    readonly WinForms.NotifyIcon _icon;
    readonly WinForms.ContextMenuStrip _menu;

    public event Action? ShowRequested, ToggleMirrorsRequested, ExitRequested;

    public TrayIcon()
    {
        _menu = new WinForms.ContextMenuStrip();
        _menu.Items.Add("Show", null, (_, _) => ShowRequested?.Invoke());
        _menu.Items.Add("Show/Hide mirrors", null, (_, _) => ToggleMirrorsRequested?.Invoke());
        _menu.Items.Add("Exit", null, (_, _) => ExitRequested?.Invoke());
        _icon = new WinForms.NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = AppInfo.Name,
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    static System.Drawing.Icon LoadAppIcon()
    {
        try
        {
            var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Icons/app.ico"));
            if (info != null)
            {
                using var stream = info.Stream;
                return new System.Drawing.Icon(stream);
            }
        }
        catch (Exception ex)
        {
            Logging.Log.Warn($"Could not load app icon: {ex.Message}");
        }
        return System.Drawing.SystemIcons.Application;
    }

    public void ShowBalloon(string title, string text) =>
        _icon.ShowBalloonTip(5000, title, text, WinForms.ToolTipIcon.Info);

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
