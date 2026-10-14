using TibiaMirror.Hotkeys;
using TibiaMirror.Models;

namespace TibiaMirror.Timers;

/// <summary>Owns the hotkey registrations of a profile's timers; Reload disposes all old ones first.</summary>
public sealed class TimerHotkeyRegistrations : IDisposable
{
    HotkeyService? _hotkeys;
    readonly Action<Guid> _trigger;
    readonly List<IDisposable> _registrations = new();

    public TimerHotkeyRegistrations(HotkeyService? hotkeys, Action<Guid> trigger) { _hotkeys = hotkeys; _trigger = trigger; }

    /// <summary>Points at a recreated HotkeyService (after a hook retry); call Reload afterwards.</summary>
    public void SetHotkeys(HotkeyService? hotkeys) { Dispose(); _hotkeys = hotkeys; }

    public void Reload(Profile profile)
    {
        Dispose();
        if (_hotkeys is not { } hk) return;
        foreach (var t in profile.Timers)
        {
            if (t.Hotkey is not { } key) continue;
            var id = t.Id;
            _registrations.Add(hk.Register(t, key, HotkeyScope.WhenTibiaFocused, () => _trigger(id)));
        }
    }

    public void Dispose()
    {
        foreach (var r in _registrations) r.Dispose();
        _registrations.Clear();
    }
}