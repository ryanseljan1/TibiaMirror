using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TibiaMirror.Mirrors;

/// <summary>Created by AppHost (AppHost.MirrorsVm); the service is attached in Start().</summary>
public sealed partial class MirrorsViewModel : ObservableObject
{
    MirrorService? _service;

    public ObservableCollection<MirrorCardViewModel> Cards { get; } = new();

    public void Attach(MirrorService service)
    {
        if (_service is not null) _service.LockedAll -= OnLockedAll;
        _service = service;
        _service.LockedAll += OnLockedAll;
        Rebuild();
        OnTibiaStateChanged();
    }

    /// <summary>Rebuilds the cards from the service's loaded profile.</summary>
    public void Rebuild()
    {
        Cards.Clear();
        if (_service is null) return;
        foreach (var r in _service.Regions) AddCard(r);
    }

    /// <summary>Raised for every card created (Task 11 fills CountdownEditor here).</summary>
    public event Action<MirrorCardViewModel>? CardCreated;

    void AddCard(TibiaMirror.Models.RegionModel r, int? index = null)
    {
        var card = new MirrorCardViewModel(_service!, r, OnCardDeleted);
        if (index is { } i) Cards.Insert(Math.Min(i, Cards.Count), card); else Cards.Add(card);
        CardCreated?.Invoke(card);
    }

    /// <summary>Removes the mirror immediately and shows "deleted - Undo"; Undo puts it back where it was.</summary>
    void OnCardDeleted(MirrorCardViewModel card)
    {
        if (_service is not { } service) return;
        var name = card.Name;
        var cardIndex = Cards.IndexOf(card);
        var removed = service.Remove(card.Id);
        Cards.Remove(card);
        if (removed is null) return;
        _snackbar?.Show($"Mirror '{name}' deleted", () =>
        {
            if (service.Restore(removed)) AddCard(removed.Region, cardIndex);
        });
    }

    public const string OpenTibiaTip = "Open Tibia to start adding mirrors";

    /// <summary>Add Region needs a visible Tibia client to select from.</summary>
    public bool CanAddRegion => _service?.CanCapture == true;

    /// <summary>Tooltip for the disabled Add Region button (null while enabled).</summary>
    public string? AddRegionTip => CanAddRegion ? null : OpenTibiaTip;

    /// <summary>True while no Tibia client is detected; drives the inline hint.</summary>
    public bool ShowTibiaHint => _service?.TibiaFound != true;

    /// <summary>Call when the Tibia state changed (AppHost.TibiaStateChanged).</summary>
    public void OnTibiaStateChanged()
    {
        OnPropertyChanged(nameof(CanAddRegion));
        OnPropertyChanged(nameof(AddRegionTip));
        OnPropertyChanged(nameof(ShowTibiaHint));
        AddRegionCommand.NotifyCanExecuteChanged();
        foreach (var c in Cards) c.RefreshCaptureAvailability();
    }

    public bool IsEmpty => Cards.Count == 0;

    void OnLockedAll() { foreach (var c in Cards) c.RefreshLocked(); }

    [RelayCommand(CanExecute = nameof(CanAddRegion))]
    async Task AddRegion()
    {
        if (_service is null) return;
        var region = await _service.AddRegionAsync();
        if (region is not null) AddCard(region);
    }

    readonly TibiaMirror.Controls.SnackbarService? _snackbar;

    public MirrorsViewModel(TibiaMirror.Controls.SnackbarService? snackbar = null)
    {
        _snackbar = snackbar;
        Cards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }
}

