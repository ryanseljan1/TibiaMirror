using TibiaMirror.Models;
namespace TibiaMirror.Tests.Models;

public class HotkeyTests
{
    [Fact] public void Display_plain_key() => Assert.Equal("F1", new Hotkey(0x70, HotkeyModifiers.None).ToString());
    [Fact] public void Display_with_modifiers_in_fixed_order() =>
        Assert.Equal("Ctrl+Shift+Alt+A", new Hotkey(0x41, HotkeyModifiers.Alt | HotkeyModifiers.Ctrl | HotkeyModifiers.Shift).ToString());
    [Fact] public void Equality_is_by_value() => Assert.Equal(new Hotkey(0x41, HotkeyModifiers.Ctrl), new Hotkey(0x41, HotkeyModifiers.Ctrl));
    [Fact] public void Json_roundtrip()
    {
        var h = new Hotkey(0x70, HotkeyModifiers.Ctrl);
        var json = System.Text.Json.JsonSerializer.Serialize(h);
        Assert.Equal(h, System.Text.Json.JsonSerializer.Deserialize<Hotkey>(json));
    }
}
