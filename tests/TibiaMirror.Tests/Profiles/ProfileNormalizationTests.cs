using TibiaMirror.Models; using TibiaMirror.Profiles; using TibiaMirror.Settings; using TibiaMirror.Timers;
namespace TibiaMirror.Tests.Profiles;

public class ProfileNormalizationTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-norm").FullName;
    public void Dispose() => Directory.Delete(_root, true);

    const string Malformed = """
        { "Name": "Bad", "Regions": [ null, { "Name": null, "Countdown": null } ],
          "Timers": [ null, { "Name": null, "Sound": null } ] }
        """;

    static void AssertClean(Profile p)
    {
        Assert.Single(p.Regions);
        Assert.NotNull(p.Regions[0].Countdown);
        Assert.False(string.IsNullOrEmpty(p.Regions[0].Name));
        Assert.Single(p.Timers);
        Assert.False(string.IsNullOrEmpty(p.Timers[0].Name));
        Assert.False(string.IsNullOrEmpty(p.Timers[0].Sound));
    }

    [Fact] public void Normalize_repairs_nulls()
    {
        var p = new Profile { Regions = null!, Timers = null! }; ProfileNormalizer.Normalize(p);
        Assert.Empty(p.Regions); Assert.Empty(p.Timers);
        var q = new Profile { Regions = { null!, new RegionModel { Name = null!, Countdown = null! } }, Timers = { null!, new AudioTimerModel { Name = null!, Sound = null! } } };
        ProfileNormalizer.Normalize(q); AssertClean(q);
    }

    [Fact] public void LoadProfile_normalizes_malformed_file()
    {
        var paths = new AppPaths(_root); var s = new ProfileService(paths); s.LoadInitial(); s.Create("Bad");
        s.Switch("Default");
        File.WriteAllText(paths.ProfileFile("Bad"), Malformed);
        s.Switch("Bad"); AssertClean(s.Current);
    }

    [Fact] public void Import_normalizes_malformed_file()
    {
        var paths = new AppPaths(_root); var s = new ProfileService(paths); s.LoadInitial();
        var f = Path.Combine(_root, "in.json"); File.WriteAllText(f, Malformed);
        Assert.Equal("Bad", s.Import(f)); s.Switch("Bad"); AssertClean(s.Current);
    }

    [Fact] public void Import_newer_schema_is_rejected()
    {
        var s = new ProfileService(new AppPaths(_root)); s.LoadInitial();
        var f = Path.Combine(_root, "new.json"); File.WriteAllText(f, "{ \"SchemaVersion\": 5, \"Name\": \"N\" }");
        Assert.Throws<ArgumentException>(() => s.Import(f));
    }

    [Fact] public void Failed_initial_load_never_overwrites_a_locked_healthy_profile()
    {
        var paths = new AppPaths(_root);
        var first = new ProfileService(paths); first.LoadInitial();
        first.Current.Regions.Add(new RegionModel { Name = "Keep" }); first.SaveCurrent();
        var second = new ProfileService(paths);
        using (new FileStream(paths.ProfileFile("Default"), FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAny<IOException>(() => second.LoadInitial());
        second.SaveCurrent();   // autosave after the failed load must not write an empty profile
        var third = new ProfileService(paths); third.LoadInitial();
        Assert.Equal("Keep", Assert.Single(third.Current.Regions).Name);
    }

    [Fact] public void Settings_normalizer_falls_back_for_empty_detection_fields()
    {
        var s = new AppSettings { TibiaProcessName = null!, TibiaTitlePrefix = "  " };
        AppSettingsNormalizer.Normalize(s);
        Assert.Equal("client", s.TibiaProcessName); Assert.Equal("Tibia", s.TibiaTitlePrefix);
    }

    [Fact] public void Settings_load_applies_fallback()
    {
        var paths = new AppPaths(_root); File.WriteAllText(paths.SettingsFile, "{ \"TibiaProcessName\": \"\", \"TibiaTitlePrefix\": null }");
        var svc = new SettingsService(paths); svc.Load();
        Assert.Equal("client", svc.Current.TibiaProcessName); Assert.Equal("Tibia", svc.Current.TibiaTitlePrefix);
    }

    [Fact] public void Startup_listening_follows_StartListeningOnOpen_both_ways()
    {
        var on = new Profile { IsListening = false, StartListeningOnOpen = true }; AudioTimersViewModel.ApplyStartupListening(on); Assert.True(on.IsListening);
        var off = new Profile { IsListening = true, StartListeningOnOpen = false }; AudioTimersViewModel.ApplyStartupListening(off); Assert.False(off.IsListening);
    }
}
