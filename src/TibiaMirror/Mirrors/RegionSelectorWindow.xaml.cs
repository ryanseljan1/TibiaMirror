using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TibiaMirror.Detection;
using TibiaMirror.Geometry;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public partial class RegionSelectorWindow : Window
{
    readonly PixelRect _client;
    readonly TaskCompletionSource<PixelRect?> _tcs = new();
    nint _hwnd;
    double _scale = 1;
    Point? _start;
    DispatcherTimer? _tooSmallTimer;

    RegionSelectorWindow(PixelRect client)
    {
        _client = client;
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            WindowStyles.SetBoundsPhysical(_hwnd, _client);
            _scale = DpiMath.Scale(Monitors.DpiForWindow(_hwnd));
        };
        ContentRendered += (_, _) => { WindowStyles.SetBoundsPhysical(_hwnd, _client); UpdateDim(null); Activate(); Focus(); };
        SizeChanged += (_, _) => UpdateDim(null);
        Closed += (_, _) => _tcs.TrySetResult(null);
    }

    /// <summary>Returns the crop relative to the Tibia client area, or null when cancelled.</summary>
    public static Task<PixelRect?> SelectAsync(TibiaState tibia)
    {
        var w = new RegionSelectorWindow(tibia.Window!.ClientBounds);
        w.Show();
        return w._tcs.Task;
    }

    void Finish(PixelRect? result)
    {
        _tcs.TrySetResult(result);
        if (IsLoaded) Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Finish(null);
        base.OnKeyDown(e);
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e) => Finish(null);

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _start = e.GetPosition(Overlay);
        CaptureMouse();
        UpdateSelection(_start.Value);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_start is not null) UpdateSelection(e.GetPosition(Overlay));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_start is null) return;
        var end = e.GetPosition(Overlay);
        var sel = ToScreen(_start.Value, end);
        _start = null;
        ReleaseMouseCapture();
        var crop = CropMath.FromSelection(sel, _client);
        if (crop.IsEmpty) { FlashTooSmall(); return; }
        Finish(crop);
    }

    PixelRect ToScreen(Point a, Point b)
    {
        int x1 = DpiMath.ToPhysical(a.X, _scale) + _client.X, y1 = DpiMath.ToPhysical(a.Y, _scale) + _client.Y;
        int x2 = DpiMath.ToPhysical(b.X, _scale) + _client.X, y2 = DpiMath.ToPhysical(b.Y, _scale) + _client.Y;
        return PixelRect.FromCorners(x1, y1, x2, y2);
    }

    void UpdateSelection(Point current)
    {
        var s = _start!.Value;
        var rect = new Rect(Math.Min(s.X, current.X), Math.Min(s.Y, current.Y), Math.Abs(current.X - s.X), Math.Abs(current.Y - s.Y));
        UpdateDim(rect);
        Outline.Visibility = Visibility.Visible;
        Canvas.SetLeft(Outline, rect.X); Canvas.SetTop(Outline, rect.Y);
        Outline.Width = rect.Width; Outline.Height = rect.Height;
        var sel = ToScreen(s, current);
        SizeLabel.Text = $"{sel.Width} \u00D7 {sel.Height}";
        SizeLabel.Visibility = Visibility.Visible;
        Canvas.SetLeft(SizeLabel, rect.X);
        Canvas.SetTop(SizeLabel, Math.Max(0, rect.Y - 22));
    }

    void UpdateDim(Rect? hole)
    {
        System.Windows.Media.Geometry geo = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        if (hole is { } h) geo = new CombinedGeometry(GeometryCombineMode.Exclude, geo, new RectangleGeometry(h));
        Dim.Data = geo;
    }

    void FlashTooSmall()
    {
        SizeLabel.Text = "Too small";
        SizeLabel.Visibility = Visibility.Visible;
        _tooSmallTimer?.Stop();
        _tooSmallTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tooSmallTimer.Tick += (_, _) =>
        {
            _tooSmallTimer!.Stop();
            SizeLabel.Visibility = Visibility.Collapsed;
            Outline.Visibility = Visibility.Collapsed;
            UpdateDim(null);
        };
        _tooSmallTimer.Start();
    }
}


