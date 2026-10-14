using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror.Profiles;

/// <summary>Small non-activating, click-through banner shown at top-center of the primary work area.</summary>
public partial class Toast : Window
{
    static Toast? _current;
    readonly DispatcherTimer _hold = new() { Interval = TimeSpan.FromMilliseconds(1500) };

    Toast(string text)
    {
        InitializeComponent();
        Label.Text = text;
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowStyles.MakeToolWindow(hwnd);
            WindowStyles.SetClickThrough(hwnd, true);
        };
        _hold.Tick += (_, _) => { _hold.Stop(); FadeOut(); };
    }

    public static void ShowToast(string text)
    {
        _current?.Close();
        var t = _current = new Toast(text);
        t.Closed += (_, _) => { if (ReferenceEquals(_current, t)) _current = null; };
        t.Show();
        t.UpdateLayout();
        var hwnd = new WindowInteropHelper(t).Handle;
        var scale = VisualTreeHelper.GetDpi(t).DpiScaleX;
        var w = (int)Math.Ceiling(t.ActualWidth * scale);
        var h = (int)Math.Ceiling(t.ActualHeight * scale);
        var wa = Monitors.PrimaryWorkArea();
        WindowStyles.SetBoundsPhysical(hwnd, new PixelRect(wa.X + (wa.Width - w) / 2, wa.Y + (int)(16 * scale), w, h));
        t._hold.Start();
    }

    void FadeOut()
    {
        var a = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
        a.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, a);
    }

    protected override void OnClosed(EventArgs e) { _hold.Stop(); base.OnClosed(e); }
}
