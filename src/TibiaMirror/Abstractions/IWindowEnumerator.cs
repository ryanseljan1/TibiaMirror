using TibiaMirror.Models;
namespace TibiaMirror.Abstractions;

public sealed record WindowInfo(nint Handle, string Title, string ProcessName, bool IsVisible, bool IsMinimized,
    PixelRect WindowBounds, PixelRect ClientBounds); // both in screen physical px

public interface IWindowEnumerator
{
    IReadOnlyList<WindowInfo> GetTopLevelWindows();
    nint GetForegroundWindow();
    int GetWindowProcessId(nint hwnd);
}
