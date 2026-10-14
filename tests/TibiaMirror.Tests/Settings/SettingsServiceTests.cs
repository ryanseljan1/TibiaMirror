using TibiaMirror.Models; using TibiaMirror.Settings; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Settings;

public class SettingsServiceTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-set").FullName;
    public void Dispose() => Directory.Delete(_root, true);

    [Fact] public void Defaults_on_first_run()
    {
        var s = new SettingsService(new AppPaths(_root)); s.Load();
        Assert.Equal("client", s.Current.TibiaProcessName); Assert.Equal(LoadStatus.Missing, s.LastLoadStatus);
    }
    [Fact] public void Roundtrip_hotkeys_and_flags()
    {
        var s = new SettingsService(new AppPaths(_root)); s.Load();
        s.Current.ShowHideAllHotkey = new Hotkey(0x7B, HotkeyModifiers.Ctrl); s.Current.OnlyShowWhenTibiaFocused = true; s.Save();
        var s2 = new SettingsService(new AppPaths(_root)); s2.Load();
        Assert.Equal(new Hotkey(0x7B, HotkeyModifiers.Ctrl), s2.Current.ShowHideAllHotkey); Assert.True(s2.Current.OnlyShowWhenTibiaFocused);
    }

    [Fact] public void Dark_mode_choice_survives_settings_load_and_save()
    {
        var paths = new AppPaths(_root);
        File.WriteAllText(paths.SettingsFile, "{ \"DarkMode\": true }");
        var settings = new SettingsService(paths);
        settings.Load();
        settings.Save();
        using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(paths.SettingsFile));
        Assert.True(json.RootElement.GetProperty("DarkMode").GetBoolean());
    }
}

public class RestoreClampTests
{
    [Fact] public void Mirrors_on_missing_monitor_are_moved_to_primary()      // Review Focus #2
    {
        var p = new Profile(); p.Regions.Add(new RegionModel { Mirror = new PixelRect(3000, 200, 300, 200) });
        RestoreClamp.ClampProfile(p, new[] { new PixelRect(0, 0, 1920, 1040) }, new PixelRect(0, 0, 1920, 1040));
        var m = p.Regions[0].Mirror; Assert.True(m.X >= 0 && m.Right <= 1920 && m.Y >= 0 && m.Bottom <= 1040);
    }
}
