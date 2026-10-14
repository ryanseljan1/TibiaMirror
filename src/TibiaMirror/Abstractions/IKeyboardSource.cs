using TibiaMirror.Models;
namespace TibiaMirror.Abstractions;

public readonly record struct KeyPress(int VirtualKey, HotkeyModifiers Modifiers);
public interface IKeyboardSource : IDisposable { event Action<KeyPress>? KeyDown; }
