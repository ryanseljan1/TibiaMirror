using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using TibiaMirror.Abstractions;
using TibiaMirror.Models;

namespace TibiaMirror.Interop;

public sealed class Win32WindowEnumerator : IWindowEnumerator
{
    const int MaxCache = 256;
    readonly Dictionary<int, string> _names = new();
    readonly HashSet<nint> _reported = new();

    public IReadOnlyList<WindowInfo> GetTopLevelWindows()
    {
        var result = new List<WindowInfo>();
        var seenPids = new HashSet<int>();
        var failing = new HashSet<nint>();
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            try
            {
                int len = NativeMethods.GetWindowTextLength(hwnd);
                if (len <= 0) return true;
                var sb = new StringBuilder(len + 1);
                NativeMethods.GetWindowText(hwnd, sb, sb.Capacity);
                var title = sb.ToString();
                if (title.Length == 0) return true;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                seenPids.Add((int)pid);
                var name = ProcessName((int)pid);

                NativeMethods.GetWindowRect(hwnd, out var wr);
                var window = new PixelRect(wr.Left, wr.Top, wr.Right - wr.Left, wr.Bottom - wr.Top);
                var client = default(PixelRect);
                if (NativeMethods.GetClientRect(hwnd, out var cr))
                {
                    var pt = new NativeMethods.POINT();
                    if (NativeMethods.ClientToScreen(hwnd, ref pt))
                        client = new PixelRect(pt.X, pt.Y, cr.Right - cr.Left, cr.Bottom - cr.Top);
                }
                result.Add(new WindowInfo(hwnd, title, name, NativeMethods.IsWindowVisible(hwnd),
                    NativeMethods.IsIconic(hwnd), window, client));
            }
            catch (Exception ex)
            {
                failing.Add(hwnd);
                if (_reported.Add(hwnd)) Logging.Log.Warn($"Window enumeration skipped {hwnd}: {ex.Message}");
            }
            return true;
        }, 0);

        _reported.IntersectWith(failing); // forget handles that vanished or recovered

        // Evict exited pids; also cap size.
        if (_names.Count > MaxCache) _names.Clear();
        else foreach (var pid in _names.Keys.Where(p => !seenPids.Contains(p)).ToList()) _names.Remove(pid);
        return result;
    }

    public nint GetForegroundWindow() => NativeMethods.GetForegroundWindow();

    public int GetWindowProcessId(nint hwnd)
    {
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return (int)pid;
    }

    string ProcessName(int pid)
    {
        if (_names.TryGetValue(pid, out var cached)) return cached;
        string name;
        try { using var p = Process.GetProcessById(pid); name = p.ProcessName; }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception) { name = ""; }
        _names[pid] = name;
        return name;
    }
}
