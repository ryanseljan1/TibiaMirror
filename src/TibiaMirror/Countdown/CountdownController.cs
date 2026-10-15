using System.Windows.Threading;
using TibiaMirror.Abstractions;
using TibiaMirror.Logging;
using TibiaMirror.Mirrors;
using TibiaMirror.Models;

namespace TibiaMirror.Countdown;

/// <summary>Glues countdowns to mirrors: hotkeys, bar windows, 16 ms ticking, flash and sound on completion.</summary>
public sealed class CountdownController : IDisposable
{
    readonly AppHost _host;
    readonly MirrorService _mirrors;
    readonly MirrorsViewModel _vm;
    readonly CountdownService _service;
    readonly CountdownHotkeyRegistrations _registrations;
    readonly Dictionary<Guid, (PixelRect Bounds, bool Shown)> _layouts = new();
    readonly Dictionary<Guid, CountdownBarWindow> _bars = new();
    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(16) };

    public CountdownController(AppHost host, MirrorService mirrors, MirrorsViewModel vm)
    {
        _host = host; _mirrors = mirrors; _vm = vm;
        _service = new CountdownService(new SystemClock());
        _registrations = new CountdownHotkeyRegistrations(host.Hotkeys, Trigger);
        _service.Completed += OnCompleted;
        _timer.Tick += (_, _) => OnTick();
        mirrors.MirrorLayoutChanged += OnLayout;
        mirrors.RegionRemoved += OnRegionRemoved;
        mirrors.RegionRestored += OnRegionRestored;
        vm.CardCreated += OnCardCreated;
        host.HotkeysReady += OnHotkeysReady;
    }

    /// <summary>Hotkey Retry recreated the HotkeyService: register the countdowns with the new one.</summary>
    void OnHotkeysReady()
    {
        void Apply() { _registrations.SetHotkeys(_host.Hotkeys); _registrations.Reload(_host.CurrentProfile); }
        if (System.Windows.Application.Current?.Dispatcher is { } d && !d.CheckAccess()) d.Invoke(Apply); else Apply();
    }

    void OnRegionRemoved(Guid id)
    {
        _service.Stop(id);
        DropBar(id);
        _layouts.Remove(id);
    }

    /// <summary>An undone delete: the restored region countdown hotkey must work again right away.</summary>
    void OnRegionRestored(Guid id) => _registrations.Reload(_host.CurrentProfile);

    void DropBar(Guid id)
    {
        if (_bars.Remove(id, out var b)) b.Close();
    }

    public CountdownService Service => _service;

    RegionModel? Find(Guid id) => _mirrors.Regions.FirstOrDefault(r => r.Id == id);

    void OnCardCreated(MirrorCardViewModel card)
    {
        if (Find(card.Id) is not { } region) return;
        card.CountdownEditor = new CountdownEditor(region, _host.Hotkeys, () => OnEdited(region), _host.Sounds.CanPlay);
    }

    /// <summary>Called after the profile's mirrors were reloaded: stops the previous profile's countdowns and re-registers hotkeys.</summary>
    public void OnProfileChanged(Profile p)
    {
        foreach (var id in _service.RunningIds) _service.Stop(id);
        foreach (var b in _bars.Values) b.Close();
        _bars.Clear();
        var live = p.Regions.Select(r => r.Id).ToHashSet();
        foreach (var id in _layouts.Keys.Where(k => !live.Contains(k)).ToList()) _layouts.Remove(id);
        _timer.Stop();
        _registrations.Reload(p);
    }

    void OnEdited(RegionModel region)
    {
        _registrations.Reload(_host.CurrentProfile);
        if (!region.Countdown.Enabled) _service.Stop(region.Id);
        UpdateBar(region.Id);
        _host.ProfileEdited();
    }

    void Trigger(Guid id)
    {
        if (Find(id) is not { } r || !r.Countdown.Enabled) return;
        var dur = double.IsNaN(r.Countdown.DurationSeconds) ? 0.5 : Math.Clamp(r.Countdown.DurationSeconds, 0.5, 3600);
        _service.Start(id, dur);
        _timer.Start();
        UpdateBar(id);
    }

    void OnLayout(Guid id, PixelRect bounds, bool shown)
    {
        _layouts[id] = (bounds, shown);
        UpdateBar(id);
    }

    void UpdateBar(Guid id)
    {
        try
        {
            var region = Find(id);
            bool want = region is { Countdown.Enabled: true } && _service.IsRunning(id)
                        && _layouts.TryGetValue(id, out var l) && l.Shown;
            if (!want || region is null)
            {
                if (_bars.TryGetValue(id, out var old)) old.HideBar();
                return;
            }
            if (!_bars.TryGetValue(id, out var bar)) _bars[id] = bar = new CountdownBarWindow();
            bar.ShowBar(region.Countdown, _layouts[id].Bounds, _service.Progress(id));
        }
        catch (Exception e) { Log.Error("Countdown bar update failed", e); }
    }

    void OnTick()
    {
        _service.Tick();
        var running = _service.RunningIds;
        foreach (var id in running)
            if (_bars.TryGetValue(id, out var bar)) bar.SetProgress(_service.Progress(id));
        if (running.Count == 0) _timer.Stop();
    }

    void OnCompleted(Guid id)
    {
        if (Find(id) is not { } r) { DropBar(id); _layouts.Remove(id); return; }
        if (_bars.TryGetValue(id, out var bar)) bar.HideBar();
        try { if (r.Countdown.FlashOnDone) _mirrors.Flash(id, 5, r.Countdown.Color); }
        catch (Exception e) { Log.Error("Countdown flash failed", e); }
        try
        {
            if (!string.IsNullOrEmpty(r.Countdown.Sound))
                _host.Sounds.Play(r.Countdown.Sound, _host.CurrentProfile.MasterVolume / 100.0);
        }
        catch (Exception e) { Log.Error("Countdown sound failed", e); }
    }

    public void Dispose()
    {
        _timer.Stop();
        _registrations.Dispose();
        _mirrors.MirrorLayoutChanged -= OnLayout;
        _mirrors.RegionRemoved -= OnRegionRemoved;
        _mirrors.RegionRestored -= OnRegionRestored;
        _vm.CardCreated -= OnCardCreated;
        _host.HotkeysReady -= OnHotkeysReady;
        _service.Completed -= OnCompleted;
        foreach (var b in _bars.Values) b.Close();
        _bars.Clear();
    }
}
