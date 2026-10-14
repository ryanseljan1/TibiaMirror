using TibiaMirror.Models; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Persistence;

public class JsonStoreRobustnessTests : IDisposable
{
    readonly string _dir = Directory.CreateTempSubdirectory("tm-robust").FullName;
    string P(string n) => Path.Combine(_dir, n);
    public void Dispose() => Directory.Delete(_dir, true);

    [Fact] public void Locked_file_propagates_io_error_and_is_not_quarantined()
    {
        JsonStore.Save(P("l.json"), new Profile { Name = "Healthy" });
        using (new FileStream(P("l.json"), FileMode.Open, FileAccess.Read, FileShare.None))
            Assert.ThrowsAny<IOException>(() => JsonStore.Load(P("l.json"), () => new Profile { Name = "Empty" }));
        Assert.Empty(Directory.GetFiles(_dir, "*.corrupt"));
        Assert.Equal("Healthy", JsonStore.Load(P("l.json"), () => new Profile()).Value.Name);
    }

    [Fact] public void Json_null_content_is_corrupt()
    {
        File.WriteAllText(P("n.json"), "null");
        Assert.Equal(LoadStatus.Corrupt, JsonStore.Load(P("n.json"), () => new Profile()).Status);
    }

    [Fact] public void Newer_schema_is_loaded_but_never_saved_over()
    {
        const string text = "{ \"SchemaVersion\": 2, \"Name\": \"Future\", \"FutureField\": 7 }";
        File.WriteAllText(P("f.json"), text);
        var r = JsonStore.Load(P("f.json"), () => new Profile());
        Assert.Equal("Future", r.Value.Name);
        JsonStore.Save(P("f.json"), r.Value);
        Assert.Equal(text, File.ReadAllText(P("f.json")));
        Assert.False(File.Exists(P("f.json.bak")));
    }

    [Fact] public void Current_schema_still_saves()
    {
        JsonStore.Save(P("ok.json"), new Profile { Name = "A" });
        JsonStore.Save(P("ok.json"), new Profile { Name = "B" });
        Assert.Contains("\"B\"", File.ReadAllText(P("ok.json")));
    }
}
