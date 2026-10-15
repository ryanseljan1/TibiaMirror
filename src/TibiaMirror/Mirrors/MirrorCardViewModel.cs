using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Windows.Media;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public sealed partial class MirrorCardViewModel : ObservableObject
{
    readonly MirrorService _service;
    readonly RegionModel _region;
    readonly Action<MirrorCardViewModel> _deleted;

    public MirrorCardViewModel(MirrorService service, RegionModel region, Action<MirrorCardViewModel> deleted)
    {
        _service = service; _region = region; _deleted = deleted;
    }

    public Guid Id => _region.Id;
    public CountdownSettings Countdown => _region.Countdown;
    /// <summary>Slot filled by Task 11 (countdown editor control).</summary>
    [ObservableProperty] object? _countdownEditor;

    public string Name
    {
        get => _region.Name;
        set { if (_region.Name == value) return; _service.Rename(Id, value); OnPropertyChanged(); }
    }

    public bool IsVisible
    {
        get => _region.IsVisible;
        set { if (_region.IsVisible == value) return; _service.SetVisible(Id, value); OnPropertyChanged(); }
    }

    public bool IsLocked
    {
        get => _region.IsLocked;
        set { if (_region.IsLocked == value) return; _service.SetLocked(Id, value); OnPropertyChanged(); }
    }

    public double Opacity
    {
        get => _region.Opacity;
        set { if (_region.Opacity == value) return; _service.SetOpacity(Id, value); OnPropertyChanged(); OnPropertyChanged(nameof(OpacityText)); }
    }

    public string OpacityText => $"{Math.Round(_region.Opacity * 100)}%";

    public void RefreshLocked() => OnPropertyChanged(nameof(IsLocked));

    /// <summary>Re-reads everything a mirror's context menu can change.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(IsLocked));
        OnPropertyChanged(nameof(IsVisible));
        OnPropertyChanged(nameof(Glow));
        OnPropertyChanged(nameof(GlowBrush));
        OnPropertyChanged(nameof(GlowChipOpacity));
    }

    public GlowSettings Glow => _region.Glow;

    /// <summary>Colour chip showing the current glow colour (dimmed while the glow is off).</summary>
    public Brush GlowBrush => GlowMenu.BrushFor(_region.Glow.Color);

    public double GlowChipOpacity => _region.Glow.Enabled ? 1.0 : 0.35;

    /// <summary>Applies new glow settings through the service (the same call the mirror's context menu uses).</summary>
    public void SetGlow(GlowSettings glow) { _service.SetGlow(Id, glow); Refresh(); }

    [RelayCommand]
    void Rename()
    {
        var dlg = new RenameDialog(Name) { Owner = Application.Current.MainWindow };
        if (dlg.ShowDialog() == true && dlg.Result is { } n) Name = n;
    }

    bool CanRecrop => _service.CanCapture;

    /// <summary>Tooltip shown on the disabled Re-crop button (null while enabled, so no tooltip appears).</summary>
    public string? RecropTip => CanRecrop ? null : MirrorsViewModel.OpenTibiaTip;

    [RelayCommand(CanExecute = nameof(CanRecrop))]
    Task Recrop() => _service.RecropAsync(Id);

    /// <summary>Called by the view model when Tibia appears or disappears.</summary>
    public void RefreshCaptureAvailability()
    {
        RecropCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(RecropTip));
    }

    /// <summary>Deletion takes effect at once; the owner removes the card and offers Undo.</summary>
    [RelayCommand]
    void Delete() => _deleted(this);
}
