using TibiaMirror.Models; using TibiaMirror.Profiles;
namespace TibiaMirror.Tests.Profiles;

public class ProfileServiceTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-prof").FullName;
    public void Dispose() => Directory.Delete(_root, true);
    ProfileService Make() { var s = new ProfileService(new AppPaths(_root)); s.LoadInitial(); return s; }

    [Fact] public void First_run_creates_Default() { var s = Make(); Assert.Equal("Default", s.Current.Name); Assert.Equal(new[] { "Default" }, s.List()); }

    [Fact] public void Create_switches_and_remembers_last()
    {
        var s = Make(); s.Create("Knight");
        Assert.Equal("Knight", s.Current.Name);
        Assert.Equal("Knight", Make().Current.Name);   // fresh service reads last_profile
    }

    [Fact] public void Duplicate_copies_regions_with_new_ids()
    {
        var s = Make(); s.Current.Regions.Add(new RegionModel { Name = "HP" }); s.SaveCurrent();
        s.Duplicate("Default", "Copy");
        Assert.Equal("Copy", s.Current.Name); Assert.Equal("HP", s.Current.Regions[0].Name);
        s.Switch("Default"); var origId = s.Current.Regions[0].Id; s.Switch("Copy");
        Assert.NotEqual(origId, s.Current.Regions[0].Id);
    }

    [Fact] public void Rename_moves_file_and_updates_current()
    {
        var s = Make(); s.Rename("Default", "Main");
        Assert.Equal(new[] { "Main" }, s.List()); Assert.Equal("Main", s.Current.Name);
    }

    [Fact] public void Cannot_delete_last_profile() { var s = Make(); Assert.Throws<InvalidOperationException>(() => s.Delete("Default")); }

    [Fact] public void Delete_current_switches_to_another()
    {
        var s = Make(); s.Create("Knight"); s.Delete("Knight");
        Assert.Equal("Default", s.Current.Name); Assert.Equal(new[] { "Default" }, s.List());
    }

    [Fact] public void SwitchNext_cycles_alphabetically()
    {
        var s = Make(); s.Create("B"); s.Create("A"); s.Switch("A");
        s.SwitchNext(); Assert.Equal("B", s.Current.Name);
        s.SwitchNext(); Assert.Equal("Default", s.Current.Name);
        s.SwitchNext(); Assert.Equal("A", s.Current.Name);
    }

    [Fact] public void Invalid_name_throws_and_creates_no_file()
    {
        var s = Make(); Assert.Throws<ArgumentException>(() => s.Create("Knight/PvP"));
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "profiles"), "*.json"));
    }

    [Fact] public void Import_renames_on_collision()
    {
        var s = Make(); var exported = Path.Combine(_root, "exp.json"); s.Export("Default", exported);
        Assert.Equal("Default (2)", s.Import(exported));
    }

    [Fact] public void Corrupt_last_profile_raises_notice()
    {
        var s = Make(); File.WriteAllText(Path.Combine(_root, "profiles", "Default.json"), "{bad");
        var s2 = new ProfileService(new AppPaths(_root)); string? msg = null; s2.Notice += m => msg = m; s2.LoadInitial();
        Assert.NotNull(msg);
    }

    [Fact] public void Import_max_length_collision_stays_valid()
    {
        var s = Make(); var name = new string('a', 40); s.Create(name);
        var f = Path.Combine(_root, "exp.json"); s.Export(name, f);
        var r = s.Import(f);
        Assert.True(r.Length <= 40); Assert.NotEqual(name, r); Assert.Contains(r, s.List());
    }

    [Fact] public void Import_null_lists_become_empty()
    {
        var s = Make(); var f = Path.Combine(_root, "n.json"); File.WriteAllText(f, "{\"Name\":\"X\",\"Regions\":null,\"Timers\":null}");
        Assert.Equal("X", s.Import(f)); s.Switch("X");
        Assert.Empty(s.Current.Regions); Assert.Empty(s.Current.Timers);
    }

    [Fact] public void Import_malformed_json_throws()
    {
        var s = Make(); var f = Path.Combine(_root, "bad.json"); File.WriteAllText(f, "{bad");
        Assert.Throws<ArgumentException>(() => s.Import(f));
    }

    [Fact] public void Duplicate_assigns_new_timer_ids()
    {
        var s = Make(); s.Current.Timers.Add(new AudioTimerModel { Name = "T" }); s.SaveCurrent();
        s.Duplicate("Default", "Copy"); var copyId = s.Current.Timers[0].Id; s.Switch("Default");
        Assert.NotEqual(copyId, s.Current.Timers[0].Id);
    }

    [Fact] public void Delete_current_updates_last_profile()
    {
        var s = Make(); s.Create("Knight"); s.Delete("Knight");
        Assert.Equal("Default", Make().Current.Name);
    }

    [Fact] public void SwitchNext_with_one_profile_keeps_it() { var s = Make(); s.SwitchNext(); Assert.Equal("Default", s.Current.Name); }

    [Fact] public void Case_only_rename_keeps_one_file()
    {
        var s = Make(); s.Rename("Default", "default");
        Assert.Equal("default", s.Current.Name);
        Assert.Single(Directory.GetFiles(Path.Combine(_root, "profiles"), "*.json"));
    }

    [Fact] public void Delete_then_restore_brings_back_identical_file()
    {
        var s = Make(); s.Create("Knight"); s.Current.Regions.Add(new RegionModel { Name = "HP" }); s.SaveCurrent(); s.Switch("Default");
        var file = Path.Combine(_root, "profiles", "Knight.json"); var before = File.ReadAllBytes(file);
        var d = s.Delete("Knight");
        Assert.DoesNotContain("Knight", s.List());
        s.Restore(d);
        Assert.Contains("Knight", s.List()); Assert.Equal(before, File.ReadAllBytes(file));
        Assert.Equal("Default", s.Current.Name);   // wasn't current, stays on Default
    }

    [Fact] public void Restore_of_deleted_current_profile_makes_it_current_again_with_latest_edits()
    {
        var s = Make(); s.Create("Knight"); s.Current.Regions.Add(new RegionModel { Name = "Unsaved" });
        var d = s.Delete("Knight");   // edits made since the last autosave must survive the round trip
        Assert.Equal("Default", s.Current.Name);
        s.Restore(d);
        Assert.Equal("Knight", s.Current.Name); Assert.Equal("Unsaved", s.Current.Regions[0].Name);
    }

    [Fact] public void Restore_fails_when_name_was_reused()
    {
        var s = Make(); s.Create("Knight"); s.Switch("Default");
        var d = s.Delete("Knight"); s.Create("Knight");
        Assert.Throws<InvalidOperationException>(() => s.Restore(d));
    }

    [Fact] public void Delete_of_last_profile_still_refused_and_returns_nothing_to_undo()
    {
        var s = Make(); Assert.Throws<InvalidOperationException>(() => s.Delete("Default"));
        Assert.Equal(new[] { "Default" }, s.List());
    }
}
