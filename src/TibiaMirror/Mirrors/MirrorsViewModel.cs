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

    void AddCard(TibiaMirror.Models.RegionModel r)
    {
        var card = new MirrorCardViewModel(_service!, r, c => Cards.Remove(c));
        Cards.Add(card);
        CardCreated?.Invoke(card);
    }

    public bool IsEmpty => Cards.Count == 0;

    void OnLockedAll() { foreach (var c in Cards) c.RefreshLocked(); }

    [RelayCommand]
    async Task AddRegion()
    {
        if (_service is null) return;
        var region = await _service.AddRegionAsync();
        if (region is not null) AddCard(region);
    }

    public MirrorsViewModel()
    {
        Cards.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }
}

