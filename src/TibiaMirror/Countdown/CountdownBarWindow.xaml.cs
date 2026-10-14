using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror.Countdown;

/// <summary>Click-through, topmost tool window drawing a bar that drains toward the top/left end of its edge.</summary>
public partial class CountdownBarWindow : Window
{
    readonly nint _hwnd;
    bool _horizontal = true;
    bool _closed;

    public CountdownBarWindow()
    {
        InitializeComponent();
        _hwnd = new WindowInteropHelper(this).EnsureHandle();
        WindowStyles.MakeToolWindow(_hwnd);
        WindowStyles.SetClickThrough(_hwnd, true);
    }

    void Configure(CountdownSettings s, PixelRect mirror)
    {
        _horizontal = s.Side is BarSide.Top or BarSide.Bottom;
        try { Fill.Fill = (Brush)new BrushConverter().ConvertFromString(s.Color)!; }
        catch { Fill.Fill = Brushes.OrangeRed; }
        WindowStyles.SetBoundsPhysical(_hwnd, CountdownBarLayout.Compute(mirror, s.Side, s.Shape, s.Thickness));
    }

    /// <summary>Fill = progress x full length, anchored to the end (bottom/right) so it drains toward top/left.</summary>
    public void SetProgress(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        Root.ColumnDefinitions.Clear();
        Root.RowDefinitions.Clear();
        if (_horizontal)
        {
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - progress, GridUnitType.Star) });
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(progress, GridUnitType.Star) });
            Grid.SetColumn(Fill, 1); Grid.SetRow(Fill, 0);
        }
        else
        {
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - progress, GridUnitType.Star) });
            Root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(progress, GridUnitType.Star) });
            Grid.SetRow(Fill, 1); Grid.SetColumn(Fill, 0);
        }
    }

    /// <summary>Applies settings and mirror bounds, then shows the window. Call SetProgress after this when settings changed.</summary>
    public void ShowBar(CountdownSettings s, PixelRect mirror, double progress)
    {
        if (_closed) return;
        Configure(s, mirror);
        SetProgress(progress);
        if (!IsVisible)
        {
            Show();
            Configure(s, mirror); // physical bounds win after first show
        }
    }

    public void HideBar() { if (!_closed && IsVisible) Hide(); }

    protected override void OnClosed(EventArgs e) { _closed = true; base.OnClosed(e); }
}
