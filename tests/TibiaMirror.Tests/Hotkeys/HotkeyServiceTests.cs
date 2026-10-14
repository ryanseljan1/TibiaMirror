using TibiaMirror.Abstractions; using TibiaMirror.Hotkeys; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Hotkeys;

sealed class FakeKeyboard : IKeyboardSource
{
    public event Action<KeyPress>? KeyDown;
    public void Press(int vk, HotkeyModifiers m = HotkeyModifiers.None) => KeyDown?.Invoke(new KeyPress(vk, m));
    public void Dispose() { }
}

public class HotkeyServiceTests
{
    readonly FakeKeyboard _kb = new();
    bool _tibiaFocused = true;
    HotkeyService Svc() => new(_kb, () => _tibiaFocused, a => a());
    const int F1 = 0x70;

    [Fact] public void Matching_key_fires()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _kb.Press(F1); Assert.Equal(1, n); }

    [Fact] public void Different_modifiers_do_not_fire()                    // Review Focus #4
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _kb.Press(F1, HotkeyModifiers.Ctrl); Assert.Equal(0, n); }

    [Fact] public void Tibia_scope_ignored_when_other_app_focused()       // Review Focus #4
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.None), HotkeyScope.WhenTibiaFocused, () => n++); _tibiaFocused = false; _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Global_scope_fires_regardless_of_focus()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, HotkeyModifiers.Ctrl), HotkeyScope.Global, () => n++); _tibiaFocused = false; _kb.Press(F1, HotkeyModifiers.Ctrl); Assert.Equal(1, n); }

    [Fact] public void Disposing_registration_stops_it()
    { int n = 0; using var s = Svc(); var r = s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++); r.Dispose(); _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Two_owners_same_key_both_fire()
    { int n = 0; using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); _kb.Press(F1); Assert.Equal(2, n); }

    [Fact] public void Suspended_service_fires_nothing()
    { int n = 0; using var s = Svc(); s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++); s.IsSuspended = true; _kb.Press(F1); Assert.Equal(0, n); }

    [Fact] public void Conflicts_exclude_own_owner()
    {
        using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => { }); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => { });
        Assert.Equal(new object[] { "b" }, s.FindConflicts(new Hotkey(F1, 0), "a"));
        Assert.Empty(s.FindConflicts(new Hotkey(F1, HotkeyModifiers.Shift), null));
    }

    [Fact] public void Callback_exception_does_not_stop_other_callbacks()
    { int n = 0; using var s = Svc(); s.Register("a", new Hotkey(F1, 0), HotkeyScope.Global, () => throw new Exception("x")); s.Register("b", new Hotkey(F1, 0), HotkeyScope.Global, () => n++); _kb.Press(F1); Assert.Equal(1, n); }

    [Fact] public void Reregistering_with_a_different_key_fires_only_the_new_key()
    {
        int a = 0, b = 0; using var s = Svc();
        var r = s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => a++); r.Dispose();
        s.Register(this, new Hotkey(F1 + 1, 0), HotkeyScope.Global, () => b++);
        _kb.Press(F1); _kb.Press(F1 + 1);
        Assert.Equal(0, a); Assert.Equal(1, b);
    }

    [Fact] public void Disposing_all_owner_registrations_then_reregistering_does_not_double_fire()
    {
        int n = 0; using var s = Svc();
        var first = new[] { s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++), s.Register(this, new Hotkey(F1 + 1, 0), HotkeyScope.Global, () => n++) };
        foreach (var r in first) r.Dispose();
        s.Register(this, new Hotkey(F1, 0), HotkeyScope.Global, () => n++);
        _kb.Press(F1); _kb.Press(F1 + 1);
        Assert.Equal(1, n);
    }

    [Fact] public void Timer_registrations_reload_replaces_previous_registrations()
    {
        using var s = Svc(); var fired = new List<Guid>();
        var t1 = new AudioTimerModel { Hotkey = new Hotkey(F1, 0) }; var t2 = new AudioTimerModel { Hotkey = new Hotkey(F1 + 1, 0) };
        var p = new Profile(); p.Timers.Add(t1); p.Timers.Add(t2);
        using var regs = new TibiaMirror.Timers.TimerHotkeyRegistrations(s, fired.Add);
        regs.Reload(p); regs.Reload(p);                 // reload must not duplicate
        _kb.Press(F1); _kb.Press(F1 + 1);
        Assert.Equal(new[] { t1.Id, t2.Id }, fired);
        fired.Clear(); t1.Hotkey = new Hotkey(F1 + 2, 0); p.Timers.Remove(t2); regs.Reload(p);
        _kb.Press(F1); _kb.Press(F1 + 1); _kb.Press(F1 + 2);
        Assert.Equal(new[] { t1.Id }, fired);
    }

    [Fact] public void Timer_registrations_with_no_hotkey_service_is_a_noop()
    {
        var p = new Profile(); p.Timers.Add(new AudioTimerModel { Hotkey = new Hotkey(F1, 0) });
        using var regs = new TibiaMirror.Timers.TimerHotkeyRegistrations(null, _ => { });
        regs.Reload(p);
    }

    [Fact] public void Conflicts_exclude_timer_being_edited()
    {
        using var s = Svc(); var t = new AudioTimerModel { Hotkey = new Hotkey(F1, 0) }; var other = new AudioTimerModel { Hotkey = new Hotkey(F1, 0) };
        s.Register(t, t.Hotkey, HotkeyScope.Global, () => { }); s.Register(other, other.Hotkey, HotkeyScope.Global, () => { });
        Assert.Equal(new object[] { other }, s.FindConflicts(new Hotkey(F1, 0), t));
    }
}