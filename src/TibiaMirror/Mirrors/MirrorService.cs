using System.Windows;
using TibiaMirror.Detection;
using TibiaMirror.Geometry;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

public sealed class MirrorService : IDisposable
{
    readonly TibiaLocator _locator;
    readonly Func<AppSettings> _settings;
    readonly Action _onChanged;
    readonly Dictionary<Guid, MirrorWindow> _windows = new();
    readonly HashSet<nint> _handles = new();
    readonly object _handlesGate = new();
    Profile _profile = new();
    bool _allHidden;

    public MirrorService(TibiaLocator locator, Func<AppSettings> settings, Action onChanged)
    {
        _locator = locator;
        _settings = settings;
        _onChanged = onChanged;
        _locator.StateChanged += OnTibiaState;
    }

    public IReadOnlyList<RegionModel> Regions => _profile.Regions;

    /// <summary>(region id, mirror bounds in screen physical px, currently shown)</summary>
    public event Action<Guid, PixelRect, bool>? MirrorLayoutChanged;

    public bool AllHidden
    {
        get => _allHidden;
        set { _allHidden = value; RefreshAll(); }
    }

    public void Load(Profile profile)
    {
        CloseAll();
        _profile = profile;
        foreach (var r in profile.Regions) Open(r);
        RefreshAll();
    }

    /// <summary>True if <paramref name="hwnd"/> is one of our mirror windows. O(1) and thread-safe (used from the hotkey focus gate).</summary>
    public bool IsMirrorWindow(nint hwnd) { lock (_handlesGate) return _handles.Contains(hwnd); }

    void OnTibiaState(TibiaState s) => RefreshAll();

    /// <summary>Re-applies visibility for the current Tibia state and settings.</summary>
    public void Refresh() => RefreshAll();

    bool _selecting;

    void RefreshAll()
    {
        foreach (var w in _windows.Values.ToList()) Refresh(w);
    }

    void Refresh(MirrorWindow w) => w.Apply(_locator.State, _allHidden, _settings().OnlyShowWhenTibiaFocused);

    void Open(RegionModel r)
    {
        var w = new MirrorWindow(r, _onChanged);
        w.LayoutChanged += () => MirrorLayoutChanged?.Invoke(r.Id, w.Bounds, w.IsShown);
        _windows[r.Id] = w;
        lock (_handlesGate) _handles.Add(w.Handle);
        Refresh(w);
    }

    void CloseAll()
    {
        foreach (var w in _windows.Values) w.CloseForReal();
        _windows.Clear();
        lock (_handlesGate) _handles.Clear();
    }

    RegionModel? Find(Guid id) => _profile.Regions.FirstOrDefault(r => r.Id == id);

    public async Task<RegionModel?> AddRegionAsync()
    {
        if (_selecting) return null;
        var state = _locator.State;
        if (!state.IsFound || state.IsMinimized)
        {
            MessageBox.Show("Start Tibia first, then click Add Region.");
            return null;
        }
        _selecting = true;
        var startProfile = _profile;   // the profile can change (Next-profile hotkey) while the selector is open
        PixelRect? crop;
        try { crop = await RegionSelectorWindow.SelectAsync(state); }
        finally { _selecting = false; }
        if (crop is not { } c) return null;
        if (!ReferenceEquals(startProfile, _profile))
        {
            TibiaMirror.Logging.Log.Warn("Profile changed while selecting a region; the new region was discarded.");
            return null;
        }
        var client = _locator.State.Window?.ClientBounds ?? state.Window!.ClientBounds;
        var region = new RegionModel
        {
            Name = RegionNaming.NextName(_profile.Regions.Select(x => x.Name)),
            Crop = c,
            Mirror = PlaceMirror(c, client),
        };
        _profile.Regions.Add(region);
        Open(region);
        _onChanged();
        return region;
    }

    static PixelRect PlaceMirror(PixelRect crop, PixelRect client) =>
        MonitorClamp.Clamp(new PixelRect(client.X + crop.Right + 20, client.Y + crop.Y, crop.Width, crop.Height),
            Monitors.WorkAreas(), Monitors.PrimaryWorkArea());

    public async Task RecropAsync(Guid id)
    {
        if (_selecting) return;
        var region = Find(id);
        var state = _locator.State;
        if (region is null) return;
        if (!state.IsFound || state.IsMinimized)
        {
            MessageBox.Show("Start Tibia first, then click Re-crop.");
            return;
        }
        _selecting = true;
        var startProfile = _profile;
        PixelRect? crop;
        try { crop = await RegionSelectorWindow.SelectAsync(state); }
        finally { _selecting = false; }
        if (!ReferenceEquals(startProfile, _profile))
        {
            TibiaMirror.Logging.Log.Warn("Profile changed while re-cropping a region; the selection was discarded.");
            return;
        }
        if (crop is not { } c || !_windows.TryGetValue(id, out var w)) return;
        region.Crop = c;
        // keep the mirror's position, adopt the new crop's size at 1:1
        var m = region.Mirror;
        w.SetBounds(MonitorClamp.Clamp(new PixelRect(m.X, m.Y, c.Width, c.Height), Monitors.WorkAreas(), Monitors.PrimaryWorkArea()));
        _onChanged();
    }

    /// <summary>Raised after a region was removed (id).</summary>
    public event Action<Guid>? RegionRemoved;

    public void Remove(Guid id)
    {
        if (_windows.Remove(id, out var w)) { lock (_handlesGate) _handles.Remove(w.Handle); w.CloseForReal(); }
        _profile.Regions.RemoveAll(r => r.Id == id);
        RegionRemoved?.Invoke(id);
        _onChanged();
    }

    public void Rename(Guid id, string name)
    {
        if (Find(id) is not { } r) return;
        r.Name = name;
        _onChanged();
    }

    public void SetVisible(Guid id, bool v)
    {
        if (Find(id) is not { } r) return;
        r.IsVisible = v;
        if (_windows.TryGetValue(id, out var w)) Refresh(w);
        _onChanged();
    }

    public void SetLocked(Guid id, bool v)
    {
        if (Find(id) is not { } r) return;
        r.IsLocked = v;
        if (_windows.TryGetValue(id, out var w)) w.ApplyLocked();
        _onChanged();
    }

    public void SetOpacity(Guid id, double o)
    {
        if (Find(id) is not { } r) return;
        r.Opacity = Math.Clamp(o, 0.1, 1.0);
        if (_windows.TryGetValue(id, out var w)) Refresh(w);
        _onChanged();
    }

    /// <summary>Locks every mirror. Raises <see cref="LockedAll"/> so cards can update.</summary>
    public void LockAll()
    {
        foreach (var r in _profile.Regions) SetLockedNoSave(r);
        LockedAll?.Invoke();
        _onChanged();
    }

    public event Action? LockedAll;

    void SetLockedNoSave(RegionModel r)
    {
        r.IsLocked = true;
        if (_windows.TryGetValue(r.Id, out var w)) w.ApplyLocked();
    }

    public void Flash(Guid id, int pulses, string colorHex)
    {
        if (_windows.TryGetValue(id, out var w)) w.Flash(pulses, colorHex);
    }

    public void Dispose()
    {
        _locator.StateChanged -= OnTibiaState;
        CloseAll();
    }
}
