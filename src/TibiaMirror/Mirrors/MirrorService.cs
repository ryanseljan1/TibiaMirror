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

    void OnTibiaState(TibiaState s) => RefreshAll();

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
        Refresh(w);
    }

    void CloseAll()
    {
        foreach (var w in _windows.Values) w.CloseForReal();
        _windows.Clear();
    }

    RegionModel? Find(Guid id) => _profile.Regions.FirstOrDefault(r => r.Id == id);

    public async Task<RegionModel?> AddRegionAsync()
    {
        var state = _locator.State;
        if (!state.IsFound || state.IsMinimized)
        {
            MessageBox.Show("Start Tibia first, then click Add Region.");
            return null;
        }
        var crop = await RegionSelectorWindow.SelectAsync(state);
        if (crop is not { } c) return null;
        var client = _locator.State.Window?.ClientBounds ?? state.Window!.ClientBounds;
        var region = new RegionModel
        {
            Name = "Region " + (_profile.Regions.Count + 1),
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
        var region = Find(id);
        var state = _locator.State;
        if (region is null) return;
        if (!state.IsFound || state.IsMinimized)
        {
            MessageBox.Show("Start Tibia first, then click Re-crop.");
            return;
        }
        var crop = await RegionSelectorWindow.SelectAsync(state);
        if (crop is not { } c || !_windows.TryGetValue(id, out var w)) return;
        region.Crop = c;
        // keep the mirror's position, adopt the new crop's size at 1:1
        var m = region.Mirror;
        w.SetBounds(MonitorClamp.Clamp(new PixelRect(m.X, m.Y, c.Width, c.Height), Monitors.WorkAreas(), Monitors.PrimaryWorkArea()));
        _onChanged();
    }

    public void Remove(Guid id)
    {
        if (_windows.Remove(id, out var w)) w.CloseForReal();
        _profile.Regions.RemoveAll(r => r.Id == id);
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
