using TibiaMirror.Models;
using TibiaMirror.Persistence;

namespace TibiaMirror.Settings;

public sealed class SettingsService(AppPaths paths)
{
    public AppSettings Current { get; private set; } = new();
    public LoadStatus LastLoadStatus { get; private set; } = LoadStatus.Missing;
    public string? CorruptPath { get; private set; }

    public void Load()
    {
        var r = JsonStore.Load(paths.SettingsFile, () => new AppSettings());
        Current = r.Value;
        LastLoadStatus = r.Status;
        CorruptPath = r.CorruptPath;
    }

    /// <summary>Can throw IOException / UnauthorizedAccessException.</summary>
    public void Save() => JsonStore.Save(paths.SettingsFile, Current);
}
