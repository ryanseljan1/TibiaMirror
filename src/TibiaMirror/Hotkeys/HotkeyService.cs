using TibiaMirror.Abstractions;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Hotkeys;

public sealed class HotkeyService : IDisposable
{
    sealed record Registration(object Owner, Hotkey Hotkey, HotkeyScope Scope, Action Callback);

    readonly IKeyboardSource _source;
    readonly Func<bool> _isTibiaFocused;
    readonly Action<Action> _dispatch;
    readonly List<Registration> _regs = new();
    readonly object _gate = new();
    volatile bool _suspended;

    public HotkeyService(IKeyboardSource source, Func<bool> isTibiaFocused, Action<Action> dispatch)
    {
        _source = source; _isTibiaFocused = isTibiaFocused; _dispatch = dispatch;
        _source.KeyDown += OnKeyDown;
    }

    /// <summary>True while a HotkeyBox is recording; no callbacks fire.</summary>
    public bool IsSuspended { get => _suspended; set => _suspended = value; }

    public IDisposable Register(object owner, Hotkey hotkey, HotkeyScope scope, Action callback)
    {
        var reg = new Registration(owner, hotkey, scope, callback);
        lock (_gate) _regs.Add(reg);
        return new Handle(this, reg);
    }

    public IReadOnlyList<object> FindConflicts(Hotkey hotkey, object? excludeOwner)
    {
        lock (_gate)
            return _regs.Where(r => Matches(r.Hotkey, hotkey.VirtualKey, hotkey.Modifiers) && !Equals(r.Owner, excludeOwner))
                        .Select(r => r.Owner).Distinct().ToList();
    }

    static bool Matches(Hotkey h, int vk, HotkeyModifiers m) => h.VirtualKey == vk && h.Modifiers == m;

    void OnKeyDown(KeyPress press)
    {
        if (_suspended) return;
        List<Registration> matches;
        lock (_gate)
            matches = _regs.Where(r => Matches(r.Hotkey, press.VirtualKey, press.Modifiers)).ToList();
        if (matches.Count == 0) return;

        if (matches.Any(r => r.Scope == HotkeyScope.WhenTibiaFocused))
        {
            bool focused;
            try { focused = _isTibiaFocused(); }
            catch (Exception e) { Log.Error("IsTibiaFocused failed", e); focused = false; }
            if (!focused) matches.RemoveAll(r => r.Scope == HotkeyScope.WhenTibiaFocused);
            if (matches.Count == 0) return;
        }

        _dispatch(() =>
        {
            foreach (var r in matches)
            {
                try { r.Callback(); }
                catch (Exception e) { Log.Error("Hotkey callback failed", e); }
            }
        });
    }

    public void Dispose() => _source.KeyDown -= OnKeyDown;

    sealed class Handle(HotkeyService owner, Registration reg) : IDisposable
    {
        public void Dispose() { lock (owner._gate) owner._regs.Remove(reg); }
    }
}
