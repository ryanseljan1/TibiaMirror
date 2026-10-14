using TibiaMirror.Models; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Persistence;

public class JsonStoreTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("tm-tests").FullName;
    string P(string n) => Path.Combine(_dir, n);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact] public void Missing_file_returns_default()
    {
        var r = JsonStore.Load(P("a.json"), () => new Profile { Name = "Default" });
        Assert.Equal(LoadStatus.Missing, r.Status); Assert.Equal("Default", r.Value.Name);
    }

    [Fact] public void Roundtrip_preserves_regions_hotkeys_and_enums()
    {
        var p = new Profile { Name = "Knight" };
        p.Regions.Add(new RegionModel { Name = "HP", Crop = new(1, 2, 30, 40), Mirror = new(5, 6, 70, 80),
            Countdown = new CountdownSettings { Hotkey = new Hotkey(0x70, HotkeyModifiers.Ctrl), Side = BarSide.Left } });
        p.Timers.Add(new AudioTimerModel { Name = "Haste", OnRepress = RepressBehavior.IgnoreWhileRunning });
        JsonStore.Save(P("k.json"), p);
        var back = JsonStore.Load(P("k.json"), () => new Profile()).Value;
        Assert.Equal(new PixelRect(1, 2, 30, 40), back.Regions[0].Crop);
        Assert.Equal(new Hotkey(0x70, HotkeyModifiers.Ctrl), back.Regions[0].Countdown.Hotkey);
        Assert.Equal(BarSide.Left, back.Regions[0].Countdown.Side);
        Assert.Equal(RepressBehavior.IgnoreWhileRunning, back.Timers[0].OnRepress);
        Assert.Contains("\"Left\"", File.ReadAllText(P("k.json")));   // enums as strings, human-readable
    }

    [Fact] public void Second_save_keeps_previous_as_bak()
    {
        JsonStore.Save(P("s.json"), new Profile { Name = "One" });
        JsonStore.Save(P("s.json"), new Profile { Name = "Two" });
        Assert.Contains("One", File.ReadAllText(P("s.json.bak")));
    }

    [Fact] public void Corrupt_file_restores_from_backup()
    {
        JsonStore.Save(P("c.json"), new Profile { Name = "Good" });
        JsonStore.Save(P("c.json"), new Profile { Name = "Good2" });
        File.WriteAllText(P("c.json"), "{ not json");
        var r = JsonStore.Load(P("c.json"), () => new Profile());
        Assert.Equal(LoadStatus.RestoredFromBackup, r.Status); Assert.Equal("Good", r.Value.Name);
    }

    [Fact] public void Corrupt_without_backup_is_quarantined()
    {
        File.WriteAllText(P("x.json"), "garbage");
        var r = JsonStore.Load(P("x.json"), () => new Profile { Name = "Fresh" });
        Assert.Equal(LoadStatus.Corrupt, r.Status); Assert.Equal("Fresh", r.Value.Name);
        Assert.False(File.Exists(P("x.json"))); Assert.True(File.Exists(r.CorruptPath));
    }

    [Fact] public void Empty_file_counts_as_corrupt()
    {
        File.WriteAllText(P("e.json"), "");
        Assert.Equal(LoadStatus.Corrupt, JsonStore.Load(P("e.json"), () => new Profile()).Status);
    }

    [Fact] public void Leftover_tmp_file_does_not_break_save()
    {
        File.WriteAllText(P("t.json.tmp"), "junk");
        JsonStore.Save(P("t.json"), new Profile { Name = "T" });
        Assert.Equal("T", JsonStore.Load(P("t.json"), () => new Profile()).Value.Name);
    }

    [Fact] public void Quarantine_move_failure_falls_back_to_default()
    {
        // access-denied is not portable to simulate; hold the file open (FileShare.Read) so the move fails instead
        File.WriteAllText(P("l.json"), "garbage");
        using var h = new FileStream(P("l.json"), FileMode.Open, FileAccess.Read, FileShare.Read);
        var r = JsonStore.Load(P("l.json"), () => new Profile { Name = "Fresh" });
        Assert.Equal(LoadStatus.Corrupt, r.Status); Assert.Equal("Fresh", r.Value.Name);
        Assert.Equal(P("l.json"), r.CorruptPath);
    }
}
