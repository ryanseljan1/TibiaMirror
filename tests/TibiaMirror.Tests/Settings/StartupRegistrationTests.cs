using TibiaMirror.Settings;
namespace TibiaMirror.Tests.Settings;

public class StartupRegistrationTests
{
    sealed class FakeRunKey : IRunKey
    {
        public Dictionary<string, string> Values = new();
        public int Writes;
        public string? Get(string name) => Values.TryGetValue(name, out var v) ? v : null;
        public void Set(string name, string value) { Values[name] = value; Writes++; }
        public void Remove(string name) { Values.Remove(name); Writes++; }
    }

    const string Exe = @"C:\Program Files\TibiaMirror\TibiaMirror.exe";
    readonly FakeRunKey _key = new();
    StartupRegistration Make() => new(_key, Exe);

    [Fact] public void BuildCommand_quotes_path_and_appends_startup_flag() =>
        Assert.Equal("\"" + Exe + "\" --startup", StartupRegistration.BuildCommand(Exe));

    [Fact] public void Enable_writes_TibiaMirror_value() { Make().SetEnabled(true); Assert.Equal(StartupRegistration.BuildCommand(Exe), _key.Values["TibiaMirror"]); }
    [Fact] public void Disable_removes_value() { _key.Values["TibiaMirror"] = "x"; Make().SetEnabled(false); Assert.Empty(_key.Values); }
    [Fact] public void IsEnabled_reflects_existing_entry() { Assert.False(Make().IsEnabled()); _key.Values["TibiaMirror"] = "\"x.exe\""; Assert.True(Make().IsEnabled()); }
    [Fact] public void Apply_does_not_rewrite_when_unchanged()
    {
        _key.Values["TibiaMirror"] = "\"installer.exe\"";
        Make().Apply(true); Assert.Equal(0, _key.Writes); Assert.Equal("\"installer.exe\"", _key.Values["TibiaMirror"]);
    }
    [Fact] public void Apply_toggles_both_ways() { var s = Make(); s.Apply(true); Assert.True(s.IsEnabled()); s.Apply(false); Assert.False(s.IsEnabled()); }

    [Theory]
    [InlineData("--startup", true)] [InlineData("--STARTUP", true)] [InlineData("--other", false)]
    public void IsStartupLaunch_detects_flag(string arg, bool expected) => Assert.Equal(expected, StartupRegistration.IsStartupLaunch(new[] { arg }));
    [Fact] public void IsStartupLaunch_false_without_args() => Assert.False(StartupRegistration.IsStartupLaunch(Array.Empty<string>()));
}
