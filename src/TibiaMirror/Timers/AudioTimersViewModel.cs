using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TibiaMirror.Hotkeys;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

public sealed class AudioTimersViewModel : ObservableObject, IDisposable
{
    readonly AppHost _host;
    readonly AudioTimerService _service;
    readonly List<IDisposable> _registrations = new();

    public AudioTimersViewModel(AppHost host, AudioTimerService service)
    {
        _host = host; _service = service;
        host.ProfileChanged += OnProfileChanged;
        // StartListeningOnOpen applies once, to the profile active at startup.
        var p = host.CurrentProfile;
        if (p.StartListeningOnOpen) p.IsListening = true;
        LoadProfile(p);
    }

    public ObservableCollection<AudioTimerCardViewModel> Cards { get; } = new();
    public AppHost Host => _host;
    public AudioTimerService Service => _service;
    public bool IsEmpty => Cards.Count == 0;

    public bool IsListening
    {
        get => _host.CurrentProfile.IsListening;
        set { if (_host.CurrentProfile.IsListening == value) return; _host.CurrentProfile.IsListening = value; OnPropertyChanged(); _host.ProfileEdited(); }
    }

    public bool StartListeningOnOpen
    {
        get => _host.CurrentProfile.StartListeningOnOpen;
        set { if (_host.CurrentProfile.StartListeningOnOpen == value) return; _host.CurrentProfile.StartListeningOnOpen = value; OnPropertyChanged(); _host.ProfileEdited(); }
    }

    public int MasterVolume
    {
        get => _host.CurrentProfile.MasterVolume;
        set { value = Math.Clamp(value, 0, 100); if (_host.CurrentProfile.MasterVolume == value) return; _host.CurrentProfile.MasterVolume = value; OnPropertyChanged(); _host.ProfileEdited(); }
    }

    void OnProfileChanged(Profile p)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } d && !d.CheckAccess()) d.Invoke(() => LoadProfile(p));
        else LoadProfile(p);
    }

    void LoadProfile(Profile p)
    {
        _service.Load(p);
        RegisterHotkeys();
        Cards.Clear();
        foreach (var t in p.Timers) Cards.Add(new AudioTimerCardViewModel(t, _service, _host.Sounds));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsListening));
        OnPropertyChanged(nameof(StartListeningOnOpen));
        OnPropertyChanged(nameof(MasterVolume));
    }

    void RegisterHotkeys()
    {
        foreach (var r in _registrations) r.Dispose();
        _registrations.Clear();
        if (_host.Hotkeys is not { } hk) return;
        foreach (var t in _host.CurrentProfile.Timers)
        {
            if (t.Hotkey is not { } key) continue;
            var id = t.Id;
            _registrations.Add(hk.Register(t, key, HotkeyScope.WhenTibiaFocused, () => _service.Trigger(id)));
        }
    }

    void Rebuild()
    {
        RegisterHotkeys();
        Cards.Clear();
        foreach (var t in _host.CurrentProfile.Timers) Cards.Add(new AudioTimerCardViewModel(t, _service, _host.Sounds));
        OnPropertyChanged(nameof(IsEmpty));
        _host.ProfileEdited();
    }

    public void AddTimer(AudioTimerModel model) { _host.CurrentProfile.Timers.Add(model); Rebuild(); }

    public void UpdateTimer(AudioTimerModel existing, AudioTimerModel edited)
    {
        existing.Name = edited.Name; existing.Hotkey = edited.Hotkey; existing.DurationSeconds = edited.DurationSeconds;
        existing.Sound = edited.Sound; existing.Volume = edited.Volume; existing.OnRepress = edited.OnRepress;
        _service.Cancel(existing.Id);
        Rebuild();
    }

    public void DeleteTimer(AudioTimerModel model)
    {
        _service.Cancel(model.Id);
        _host.CurrentProfile.Timers.Remove(model);
        Rebuild();
    }

    public void Test(AudioTimerModel model) => _service.TestSound(model.Id);

    /// <summary>Called every 100 ms by AppHost after AudioTimerService.Tick().</summary>
    public void Refresh() { foreach (var c in Cards) c.Refresh(); }

    public void Dispose()
    {
        _host.ProfileChanged -= OnProfileChanged;
        foreach (var r in _registrations) r.Dispose();
        _registrations.Clear();
    }
}
