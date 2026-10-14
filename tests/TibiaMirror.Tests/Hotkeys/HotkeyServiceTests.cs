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
}
