using TibiaMirror.Models;
using TibiaMirror.Persistence;

namespace TibiaMirror.Settings;

public sealed class SettingsService(AppPaths paths)
{
    bool _loadFailed;   // the file could not be read: defaults are a placeholder and must not overwrite it

    public AppSettings Current { get; private set; } = new();
    public LoadStatus LastLoadStatus { get; private set; } = LoadStatus.Missing;
    public string? CorruptPath { get; private set; }

    public void Load()
    {
        LoadResult<AppSettings> r;
        try { r = JsonStore.Load(paths.SettingsFile, () => new AppSettings()); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            TibiaMirror.Logging.Log.Error("Settings file could not be read; using defaults without saving", e);
            _loadFailed = true;
            Current = new AppSettings();
            return;
        }
        _loadFailed = false;
        Current = r.Value;
        AppSettingsNormalizer.Normalize(Current);
        LastLoadStatus = r.Status;
        CorruptPath = r.CorruptPath;
    }

    /// <summary>Can throw IOException / UnauthorizedAccessException.</summary>
    public void Save()
    {
        if (_loadFailed) return;
        JsonStore.Save(paths.SettingsFile, Current);
    }
}
