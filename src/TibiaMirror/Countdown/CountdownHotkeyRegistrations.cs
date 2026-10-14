using TibiaMirror.Hotkeys;
using TibiaMirror.Models;

namespace TibiaMirror.Countdown;

/// <summary>Hotkey owner for a region's countdown; equal per region so editors can exclude their own registration.</summary>
public sealed class CountdownHotkeyOwner(RegionModel region)
{
    public RegionModel Region { get; } = region;
    public override bool Equals(object? obj) => obj is CountdownHotkeyOwner o && ReferenceEquals(o.Region, Region);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Region);
    public override string ToString() => Region.Name + " countdown";
}

/// <summary>Owns the hotkey registrations of a profile's countdowns; Reload disposes all old ones first.</summary>
public sealed class CountdownHotkeyRegistrations : IDisposable
{
    HotkeyService? _hotkeys;
    readonly Action<Guid> _trigger;
    readonly List<IDisposable> _registrations = new();

    public CountdownHotkeyRegistrations(HotkeyService? hotkeys, Action<Guid> trigger) { _hotkeys = hotkeys; _trigger = trigger; }

    public void SetHotkeys(HotkeyService? hotkeys) { Dispose(); _hotkeys = hotkeys; }

    public void Reload(Profile profile)
    {
        Dispose();
        if (_hotkeys is not { } hk) return;
        foreach (var r in profile.Regions)
        {
            if (!r.Countdown.Enabled || r.Countdown.Hotkey is not { } key) continue;
            var id = r.Id;
            _registrations.Add(hk.Register(new CountdownHotkeyOwner(r), key, HotkeyScope.WhenTibiaFocused, () => _trigger(id)));
        }
    }

    public void Dispose()
    {
        foreach (var r in _registrations) r.Dispose();
        _registrations.Clear();
    }
}
