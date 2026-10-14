using TibiaMirror.Abstractions;
using TibiaMirror.Models;

namespace TibiaMirror.Detection;

public sealed class TibiaLocator(IWindowEnumerator windows, Func<AppSettings> settings, Func<nint, bool> isOwnWindow)
{
    public TibiaState State { get; private set; } = new(null, false);
    public event Action<TibiaState>? StateChanged;

    public void Poll()
    {
        var s = settings();
        var match = windows.GetTopLevelWindows().FirstOrDefault(w =>
            w.IsVisible
            && string.Equals(w.ProcessName, s.TibiaProcessName, StringComparison.OrdinalIgnoreCase)
            && w.Title.StartsWith(s.TibiaTitlePrefix, StringComparison.Ordinal));
        var fg = windows.GetForegroundWindow();
        var focused = FocusRule.IsFocused(fg, match?.Handle, isOwnWindow);
        var next = new TibiaState(match, focused);
        if (next == State) return;
        State = next;
        StateChanged?.Invoke(next);
    }
}
