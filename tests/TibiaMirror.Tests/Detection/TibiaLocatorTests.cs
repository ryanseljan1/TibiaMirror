using TibiaMirror.Abstractions; using TibiaMirror.Detection; using TibiaMirror.Models;
namespace TibiaMirror.Tests.Detection;

sealed class FakeWindows : IWindowEnumerator
{
    public List<WindowInfo> Windows = new(); public nint Foreground; public Dictionary<nint, int> Pids = new();
    public IReadOnlyList<WindowInfo> GetTopLevelWindows() => Windows;
    public nint GetForegroundWindow() => Foreground;
    public int GetWindowProcessId(nint h) => Pids.TryGetValue(h, out var p) ? p : 0;
}

public class TibiaLocatorTests
{
    static WindowInfo Win(nint h, string title, string proc, bool visible = true, bool min = false) =>
        new(h, title, proc, visible, min, new PixelRect(0, 0, 800, 600), new PixelRect(8, 31, 784, 561));
    readonly FakeWindows _w = new();
    readonly AppSettings _s = new();
    TibiaLocator Make(Func<nint, bool>? own = null) => new(_w, () => _s, own ?? (_ => false));

    [Fact] public void Finds_tibia_client_by_process_and_title()
    {
        _w.Windows.Add(Win(1, "Tibia - Knight Bob", "client"));
        var l = Make(); l.Poll(); Assert.Equal((nint?)1, l.State.Window?.Handle);
    }
    [Fact] public void Ignores_wrong_process_with_tibia_title() { _w.Windows.Add(Win(1, "Tibia wiki - Chrome", "chrome")); var l = Make(); l.Poll(); Assert.False(l.State.IsFound); }
    [Fact] public void Ignores_launcher_and_invisible_windows()
    {
        _w.Windows.Add(Win(1, "Tibia Launcher", "client_launcher")); _w.Windows.Add(Win(2, "Tibia", "client", visible: false));
        var l = Make(); l.Poll(); Assert.False(l.State.IsFound);
    }
    [Fact] public void Process_match_is_case_insensitive_and_configurable()
    {
        _s.TibiaProcessName = "MyClient"; _w.Windows.Add(Win(1, "Tibia", "myclient"));
        var l = Make(); l.Poll(); Assert.True(l.State.IsFound);
    }
    [Fact] public void Raises_lost_when_window_disappears()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        TibiaState? last = null; l.StateChanged += s => last = s;
        _w.Windows.Clear(); l.Poll(); Assert.False(last!.IsFound);
    }
    [Fact] public void Focus_true_when_tibia_or_own_window_foreground()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(h => h == 99);
        _w.Foreground = 1; l.Poll(); Assert.True(l.State.IsFocused);
        _w.Foreground = 99; l.Poll(); Assert.True(l.State.IsFocused);
        _w.Foreground = 5; l.Poll(); Assert.False(l.State.IsFocused);
    }
    [Fact] public void No_event_when_nothing_changed()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        int n = 0; l.StateChanged += _ => n++; l.Poll(); l.Poll(); Assert.Equal(0, n);
    }
    [Fact] public void Event_when_window_moves()
    {
        _w.Windows.Add(Win(1, "Tibia", "client")); var l = Make(); l.Poll();
        int n = 0; l.StateChanged += _ => n++;
        _w.Windows[0] = _w.Windows[0] with { WindowBounds = new(50, 50, 800, 600), ClientBounds = new(58, 81, 784, 561) };
        l.Poll(); Assert.Equal(1, n);
    }
    [Fact] public void Minimized_is_reported()
    {
        _w.Windows.Add(Win(1, "Tibia", "client", min: true)); var l = Make(); l.Poll(); Assert.True(l.State.IsMinimized);
    }
}
