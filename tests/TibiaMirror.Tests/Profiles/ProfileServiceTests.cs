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
}
