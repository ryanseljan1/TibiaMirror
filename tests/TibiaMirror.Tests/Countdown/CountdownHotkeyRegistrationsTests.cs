using TibiaMirror.Countdown; using TibiaMirror.Hotkeys; using TibiaMirror.Models; using TibiaMirror.Tests.Hotkeys;
namespace TibiaMirror.Tests.Countdown;

public class CountdownHotkeyRegistrationsTests
{
    readonly FakeKeyboard _kb = new();
    readonly List<Guid> _fired = new();
    const int F1 = 0x70, F2 = 0x71;
    HotkeyService Svc() => new(_kb, () => true, a => a());
    static RegionModel Region(int? vk, bool enabled = true)
    {
        var r = new RegionModel();
        r.Countdown.Enabled = enabled;
        if (vk is { } k) r.Countdown.Hotkey = new Hotkey(k, HotkeyModifiers.None);
        return r;
    }

    [Fact] public void Reload_twice_leaves_one_registration_per_region()
    {
        using var s = Svc(); using var regs = new CountdownHotkeyRegistrations(s, _fired.Add);
        var p = new Profile(); var r = Region(F1); p.Regions.Add(r);
        regs.Reload(p); regs.Reload(p);
        _kb.Press(F1);
        Assert.Equal(new[] { r.Id }, _fired);
    }

    [Fact] public void Disabled_and_keyless_regions_are_skipped()
    {
        using var s = Svc(); using var regs = new CountdownHotkeyRegistrations(s, _fired.Add);
        var p = new Profile(); var on = Region(F1); p.Regions.Add(on); p.Regions.Add(Region(F2, enabled: false)); p.Regions.Add(Region(null));
        regs.Reload(p);
        _kb.Press(F1); _kb.Press(F2);
        Assert.Equal(new[] { on.Id }, _fired);
    }

    [Fact] public void Dispose_unregisters()
    {
        using var s = Svc(); var regs = new CountdownHotkeyRegistrations(s, _fired.Add);
        var p = new Profile(); p.Regions.Add(Region(F1));
        regs.Reload(p); regs.Dispose();
        _kb.Press(F1);
        Assert.Empty(_fired);
    }
}
