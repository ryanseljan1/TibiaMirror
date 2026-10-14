namespace TibiaMirror.Hotkeys;

/// <summary>Registration owner for an app-wide hotkey; ToString() is what conflict prompts show to the user.</summary>
public sealed class GlobalHotkeyOwner(string label)
{
    public override string ToString() => label;
}
