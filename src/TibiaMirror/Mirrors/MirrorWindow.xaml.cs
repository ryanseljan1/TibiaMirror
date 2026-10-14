using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using TibiaMirror.Detection;
using TibiaMirror.Geometry;
using TibiaMirror.Interop;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public partial class MirrorWindow : Window
{
    const int Inset = 2;
    static readonly Brush Accent = Freeze(new SolidColorBrush(Color.FromRgb(0xFF, 0x7F, 0x00)));
    static readonly Brush NoBrush = Brushes.Transparent;

    readonly RegionModel _region;
    readonly Action _onUserEdit;
    nint _hwnd;
    DwmThumbnail? _thumb;
    nint _thumbSource;
    bool _failureLogged;
    bool _closingForReal, _closed;
    bool _wantShown;
    DispatcherTimer? _flashTimer;
    TibiaState _state = new(null, false);

    public MirrorWindow(RegionModel region, Action onUserEdit)
    {
        _region = region;
        _onUserEdit = onUserEdit;
        InitializeComponent();
        _hwnd = new WindowInteropHelper(this).EnsureHandle();
        WindowStyles.MakeToolWindow(_hwnd);
        ApplyBounds();
        ApplyLocked();
        MouseLeftButtonDown += OnBodyMouseDown;
        foreach (var h in new[] { HandleTL, HandleTR, HandleBL, HandleBR }) WireHandle(h);
        SizeChanged += (_, _) => { if (IsVisible) { UpdateThumbnail(); } };
        // lets dependents (countdown bar) follow while dragging/resizing
        LocationChanged += (_, _) => { if (IsVisible) LayoutChanged?.Invoke(); };
    }

    public Guid RegionId => _region.Id;
    public nint Handle => _hwnd;
    public bool IsShown => IsVisible;
    public PixelRect Bounds => WindowStyles.GetBoundsPhysical(_hwnd);
    /// <summary>Raised on move/resize/show/hide.</summary>
    public event Action? LayoutChanged;

    static Brush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

    void ApplyBounds() { if (!_region.Mirror.IsEmpty) WindowStyles.SetBoundsPhysical(_hwnd, _region.Mirror); }

    public void SetBounds(PixelRect r)
    {
        _region.Mirror = r;
        WindowStyles.SetBoundsPhysical(_hwnd, r);
        UpdateThumbnail();
        LayoutChanged?.Invoke();
    }

    public void ApplyLocked()
    {
        WindowStyles.SetClickThrough(_hwnd, _region.IsLocked);
        Frame.BorderBrush = _region.IsLocked ? NoBrush : Accent;
        Handles.Visibility = _region.IsLocked ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>Re-evaluates visibility and the thumbnail for the given Tibia state.</summary>
    public void Apply(TibiaState state, bool allHidden, bool onlyWhenFocused)
    {
        _state = state;
        _wantShown = MirrorVisibility.ShouldShow(_region, state, allHidden, onlyWhenFocused);
        if (_closed) return;
        if (_wantShown && !IsVisible)
        {
            Show();
            ApplyBounds(); // WPF may size on first show; physical bounds win
            Raise();
        }
        else if (!_wantShown && IsVisible)
        {
            Hide();
            Raise();
        }
        UpdateThumbnail();
    }

    void Raise() => LayoutChanged?.Invoke();

    void UpdateThumbnail()
    {
        if (_closed) return;
        var w = _state.Window;
        if (w is null || !_wantShown) { ShowPlaceholder(false); return; }
        try
        {
            if (_thumb is null || _thumbSource != w.Handle)
            {
                _thumb?.Dispose();
                _thumb = null;
                _thumb = new DwmThumbnail(_hwnd, w.Handle);
                _thumbSource = w.Handle;
            }
            var src = CropMath.ToThumbnailSource(_region.Crop, w.WindowBounds, w.ClientBounds);
            var b = Bounds;
            var interior = new PixelRect(Inset, Inset, Math.Max(1, b.Width - 2 * Inset), Math.Max(1, b.Height - 2 * Inset));
            var dest = MirrorLayout.DestinationFor(_region.Crop, new PixelRect(0, 0, w.ClientBounds.Width, w.ClientBounds.Height), interior);
            if (src.IsEmpty || dest.IsEmpty)
            {
                // crop entirely outside the client area: drop the thumbnail (re-registered when it is visible again)
                _thumb.Dispose();
                _thumb = null;
                ShowPlaceholder(true);
            }
            else
            {
                _thumb.Update(src, dest, _region.Opacity, true);
                ShowPlaceholder(false);
            }
            _failureLogged = false;
        }
        catch (Exception e)
        {
            if (!_failureLogged) { Log.Warn($"Mirror '{_region.Name}' thumbnail failed: {e.Message}"); _failureLogged = true; }
            _thumb?.Dispose();
            _thumb = null;
            ShowPlaceholder(true);
        }
    }

    void ShowPlaceholder(bool on) => Placeholder.Visibility = on ? Visibility.Visible : Visibility.Collapsed;

    // ----- interaction -----
    void OnBodyMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_region.IsLocked || e.ChangedButton != MouseButton.Left) return;
        try { DragMove(); } catch (InvalidOperationException) { return; }
        Persist();
    }

    void Persist()
    {
        _region.Mirror = Bounds;
        UpdateThumbnail();
        _onUserEdit();
        LayoutChanged?.Invoke();
    }

    void WireHandle(Rectangle h)
    {
        PixelRect start = default;
        System.Windows.Point origin = default;
        double aspect = 1;
        bool dragging = false;

        h.MouseLeftButtonDown += (_, e) =>
        {
            if (_region.IsLocked) return;
            start = Bounds;
            origin = h.PointToScreen(e.GetPosition(h));
            aspect = _region.Crop.IsEmpty ? start.Width / (double)Math.Max(1, start.Height) : _region.Crop.Width / (double)_region.Crop.Height;
            dragging = true;
            h.CaptureMouse();
            e.Handled = true;
        };
        h.MouseMove += (_, e) =>
        {
            if (!dragging) return;
            var p = h.PointToScreen(e.GetPosition(h));
            int dx = (int)Math.Round(p.X - origin.X), dy = (int)Math.Round(p.Y - origin.Y);
            bool left = h == HandleTL || h == HandleBL, top = h == HandleTL || h == HandleTR;
            int pw = Math.Max(CropMath.MinSize, start.Width + (left ? -dx : dx));
            int ph = Math.Max(CropMath.MinSize, start.Height + (top ? -dy : dy));
            var size = (Keyboard.Modifiers & ModifierKeys.Shift) != 0
                ? start with { Width = pw, Height = ph }
                : CropMath.ResizeKeepingAspect(start, pw, ph, aspect);
            var r = size with
            {
                X = left ? start.Right - size.Width : start.X,
                Y = top ? start.Bottom - size.Height : start.Y,
            };
            WindowStyles.SetBoundsPhysical(_hwnd, r);
            UpdateThumbnail();
        };
        h.MouseLeftButtonUp += (_, e) =>
        {
            if (!dragging) return;
            dragging = false;
            h.ReleaseMouseCapture();
            Persist();
            e.Handled = true;
        };
    }

    public void Flash(int pulses, string colorHex)
    {
        Brush color;
        try { color = (Brush)new BrushConverter().ConvertFromString(colorHex)!; }
        catch { color = Accent; }
        _flashTimer?.Stop();
        int ticks = pulses * 2, n = 0;
        _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        Frame.BorderBrush = color;
        _flashTimer.Tick += (_, _) =>
        {
            n++;
            if (n >= ticks) { _flashTimer!.Stop(); RestoreBorder(); return; }
            Frame.BorderBrush = n % 2 == 0 ? color : RestingBrush();
        };
        _flashTimer.Start();
    }

    Brush RestingBrush() => _region.IsLocked ? NoBrush : Accent;
    void RestoreBorder() => Frame.BorderBrush = RestingBrush();

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_closingForReal) { e.Cancel = true; return; }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        _closed = true;
        base.OnClosed(e);
    }

    public void CloseForReal()
    {
        _closingForReal = true;
        _flashTimer?.Stop();
        _thumb?.Dispose();
        _thumb = null;
        Close();
    }
}
