using System.Windows.Threading;
using TibiaMirror.Abstractions;
using TibiaMirror.Detection;
using TibiaMirror.Geometry;
using TibiaMirror.Interop;
using TibiaMirror.Models;

namespace TibiaMirror.Mirrors;

/// <summary>What <see cref="MirrorService.Remove"/> took out, for undo.</summary>
public sealed record RemovedRegion(Profile Profile, RegionModel Region, int Index);

public sealed class MirrorService : IDisposable
{
    readonly TibiaLocator _locator;
    readonly Func<AppSettings> _settings;
    readonly Action _onChanged;
    readonly Dictionary<Guid, MirrorWindow> _windows = new();
    readonly HashSet<nint> _handles = new();
    readonly object _handlesGate = new();
    readonly FocusDebounce _focus;
    readonly DispatcherTimer _recheck = new();   // re-evaluates visibility when a debounced hide comes due
    Profile _profile = new();
    bool _allHidden;

    public MirrorService(TibiaLocator locator, Func<AppSettings> settings, Action onChanged, IClock? clock = null)
    {
        _locator = locator;
        _settings = settings;
        _onChanged = onChanged;
        _focus = new FocusDebounce(clock ?? new SystemClock());
        _focus.Reset(_locator.State.IsFocused);
        _recheck.Tick += (_, _) => { _recheck.Stop(); RefreshAll(); };
        _locator.StateChanged += OnTibiaState;
    }

    /// <summary>True when Tibia is running and not minimized, i.e. a region can be selected.</summary>
    public bool CanCapture => _locator.State is { IsFound: true, IsMinimized: false };

    /// <summary>True when the Tibia client window was found at all.</summary>
    public bool TibiaFound => _locator.State.IsFound;

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

    void Refresh(MirrorWindow w)
    {
        var onlyFocused = _settings().OnlyShowWhenTibiaFocused;
        w.Apply(EffectiveState(onlyFocused), _allHidden, onlyFocused);
        ScheduleRecheck();
    }

    /// <summary>The located state with focus passed through the hide debounce (a no-op when the setting is off).</summary>
    TibiaState EffectiveState(bool onlyFocused)
    {
        var s = _locator.State;
        if (!onlyFocused) { _focus.Reset(s.IsFocused); return s; }
        return s with { IsFocused = _focus.Filter(s.IsFocused) };
    }

    void ScheduleRecheck()
    {
        _recheck.Stop();
        if (_focus.NextCheck is not { } due) return;
        _recheck.Interval = due + TimeSpan.FromMilliseconds(20);
        _recheck.Start();
    }

    void Open(RegionModel r)
    {
        var w = new MirrorWindow(r, _onChanged, this);
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
        if (!state.IsFound || state.IsMinimized) return null;   // the UI disables Add Region in this state
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
        _focus.NotifyRegionChanged();   // selecting steals focus from Tibia: don't hide the new mirror straight away
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
        if (!state.IsFound || state.IsMinimized) return;   // the UI disables Re-crop in this state
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
        _focus.NotifyRegionChanged();
        // keep the mirror's position, adopt the new crop's size at 1:1
        var m = region.Mirror;
        w.SetBounds(MonitorClamp.Clamp(new PixelRect(m.X, m.Y, c.Width, c.Height), Monitors.WorkAreas(), Monitors.PrimaryWorkArea()));
        _onChanged();
    }

    /// <summary>Raised after a removed region was put back (id), so dependents can re-register its hotkeys.</summary>
    public event Action<Guid>? RegionRestored;

    /// <summary>Raised after a region was removed (id).</summary>
    public event Action<Guid>? RegionRemoved;

    /// <summary>Removes a region right away and returns a token for <see cref="Restore"/> (null if it did not exist).</summary>
    public RemovedRegion? Remove(Guid id)
    {
        var index = _profile.Regions.FindIndex(r => r.Id == id);
        var region = index >= 0 ? _profile.Regions[index] : null;
        if (_windows.Remove(id, out var w)) { lock (_handlesGate) _handles.Remove(w.Handle); w.CloseForReal(); }
        _profile.Regions.RemoveAll(r => r.Id == id);
        RegionRemoved?.Invoke(id);
        _onChanged();
        return region is null ? null : new RemovedRegion(_profile, region, index);
    }

    /// <summary>Puts a removed region back at its old position with its settings; its window is re-shown if it was visible.
    /// Returns false if the profile changed in the meantime.</summary>
    public bool Restore(RemovedRegion removed)
    {
        if (!ReferenceEquals(removed.Profile, _profile) || Find(removed.Region.Id) is not null) return false;
        _profile.Regions.Insert(Math.Min(removed.Index, _profile.Regions.Count), removed.Region);
        Open(removed.Region);
        RegionRestored?.Invoke(removed.Region.Id);
        _onChanged();
        return true;
    }

    /// <summary>Raised when a region's lock/visibility/glow changed so open cards can refresh (also fired for edits made from a mirror's context menu).</summary>
    public event Action<Guid>? RegionEdited;

    /// <summary>Raised when a mirror's context menu asks for deletion; the owner runs the same delete + Undo flow as the card.</summary>
    public event Action<Guid>? DeleteRequested;

    /// <summary>Deletes through <see cref="DeleteRequested"/> so the undo snackbar is offered; removes outright if nobody listens.</summary>
    public void RequestDelete(Guid id)
    {
        if (DeleteRequested is { } handler) handler(id); else Remove(id);
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
        RegionEdited?.Invoke(id);
        _onChanged();
    }

    public void SetLocked(Guid id, bool v)
    {
        if (Find(id) is not { } r) return;
        r.IsLocked = v;
        if (_windows.TryGetValue(id, out var w)) w.ApplyLocked();
        RegionEdited?.Invoke(id);
        _onChanged();
    }

    /// <summary>Stores the glow border settings (repaired) and redraws the mirror.</summary>
    public void SetGlow(Guid id, GlowSettings glow)
    {
        if (Find(id) is not { } r) return;
        r.Glow = GlowStyle.Sanitize(glow);
        if (_windows.TryGetValue(id, out var w)) w.ApplyGlow();
        RegionEdited?.Invoke(id);
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
    public void LockAll() => SetAllLocked(true);

    public void UnlockAll() => SetAllLocked(false);

    /// <summary>Hotkey behaviour: locks all if any visible mirror is unlocked, otherwise unlocks all. Returns the new state (true = locked).</summary>
    public bool ToggleLockAll()
    {
        var lockThem = MirrorLockToggle.ShouldLock(_profile.Regions);
        SetAllLocked(lockThem);
        return lockThem;
    }

    void SetAllLocked(bool locked)
    {
        foreach (var r in _profile.Regions) SetLockedNoSave(r, locked);
        LockedAll?.Invoke();
        _onChanged();
    }

    public event Action? LockedAll;

    void SetLockedNoSave(RegionModel r, bool locked)
    {
        r.IsLocked = locked;
        if (_windows.TryGetValue(r.Id, out var w)) w.ApplyLocked();
    }

    public void Flash(Guid id, int pulses, string colorHex)
    {
        if (_windows.TryGetValue(id, out var w)) w.Flash(pulses, colorHex);
    }

    public void Dispose()
    {
        _recheck.Stop();
        _locator.StateChanged -= OnTibiaState;
        CloseAll();
    }
}
