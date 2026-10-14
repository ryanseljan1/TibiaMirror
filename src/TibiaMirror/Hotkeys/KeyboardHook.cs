using System.Runtime.InteropServices;
using TibiaMirror.Abstractions;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Hotkeys;

/// <summary>
/// Observing WH_KEYBOARD_LL hook. Never swallows input: the hook procedure always returns CallNextHookEx.
/// Must be created on a thread with a message loop (the UI thread).
/// </summary>
public sealed class KeyboardHook : IKeyboardSource
{
    [DllImport("user32.dll", SetLastError = true)] static extern nint SetWindowsHookEx(int idHook, LowLevelKeyboardProc fn, nint hMod, uint threadId);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(nint hhk);
    [DllImport("user32.dll")] static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);
    [DllImport("user32.dll")] static extern short GetKeyState(int vk);
    [DllImport("kernel32.dll")] static extern nint GetModuleHandle(string? name);
    delegate nint LowLevelKeyboardProc(int nCode, nint wParam, nint lParam);
    const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;

    readonly LowLevelKeyboardProc _proc; // field keeps the delegate alive
    readonly HashSet<int> _down = new();
    nint _hook;

    public event Action<KeyPress>? KeyDown;

    public KeyboardHook()
    {
        _proc = HookProc;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        if (_hook == 0)
            throw new InvalidOperationException($"SetWindowsHookEx failed (error {Marshal.GetLastWin32Error()})");
    }

    static bool IsModifierKey(int vk) => vk is (>= 0x10 and <= 0x12) or (>= 0xA0 and <= 0xA5) or 0x5B or 0x5C;

    static bool IsDown(int vk) => (GetKeyState(vk) & 0x8000) != 0;

    nint HookProc(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            try
            {
                int msg = (int)wParam;
                int vk = Marshal.ReadInt32(lParam);
                if (msg is WM_KEYUP or WM_SYSKEYUP)
                {
                    _down.Remove(vk);
                }
                else if (msg is WM_KEYDOWN or WM_SYSKEYDOWN && !IsModifierKey(vk) && _down.Add(vk))
                {
                    var m = HotkeyModifiers.None;
                    if (IsDown(0x11)) m |= HotkeyModifiers.Ctrl;
                    if (IsDown(0x10)) m |= HotkeyModifiers.Shift;
                    if (IsDown(0x12)) m |= HotkeyModifiers.Alt;
                    if (IsDown(0x5B) || IsDown(0x5C)) m |= HotkeyModifiers.Win;
                    KeyDown?.Invoke(new KeyPress(vk, m));
                }
            }
            catch (Exception e) { Log.Error("Keyboard hook handler failed", e); }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hook != 0) { UnhookWindowsHookEx(_hook); _hook = 0; }
    }
}
