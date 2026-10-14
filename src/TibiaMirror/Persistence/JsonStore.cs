using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace TibiaMirror.Persistence;

public static class JsonStore
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Loads <paramref name="path"/>. Only unreadable CONTENT (invalid JSON, unsupported shape, null) is treated as corruption and
    /// quarantined; IO errors (a locked file, for example) propagate so a healthy file is never replaced by defaults.
    /// </summary>
    public static LoadResult<T> Load<T>(string path, Func<T> createDefault) where T : class
    {
        var result = LoadCore(path, createDefault);
        if (SchemaInfo.IsNewer(result.Value))
            TibiaMirror.Logging.Log.Warn($"{Path.GetFileName(path)} was written by a newer version of TibiaMirror (schema {((IVersioned)result.Value).SchemaVersion}); it will not be saved over.");
        return result;
    }

    static LoadResult<T> LoadCore<T>(string path, Func<T> createDefault) where T : class
    {
        var bakPath = path + ".bak";
        if (!File.Exists(path))
            return File.Exists(bakPath) && TryRead<T>(bakPath, out var b)
                ? new(b!, LoadStatus.RestoredFromBackup, null)
                : new(createDefault(), LoadStatus.Missing, null);
        if (TryRead<T>(path, out var v)) return new(v!, LoadStatus.Ok, null);
        var corrupt = path + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".corrupt";
        try { File.Move(path, corrupt, overwrite: true); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { corrupt = path; }
        if (File.Exists(bakPath) && TryRead<T>(bakPath, out var bak))
            return new(bak!, LoadStatus.RestoredFromBackup, corrupt);
        return new(createDefault(), LoadStatus.Corrupt, corrupt);
    }

    /// <summary>False only when the content is unusable. IO errors are retried briefly, then propagate.</summary>
    static bool TryRead<T>(string path, out T? value) where T : class
    {
        var text = ReadAllTextWithRetry(path);
        try { value = JsonSerializer.Deserialize<T>(text, Options); return value is not null; }
        catch (Exception e) when (e is JsonException or NotSupportedException) { value = null; return false; }
    }

    static string ReadAllTextWithRetry(string path)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { return File.ReadAllText(path); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException && attempt < 3) { Thread.Sleep(50); }
        }
    }

    /// <summary>Atomic, durable save. Skipped (with no error) for models from a newer schema so they are never downgraded.</summary>
    public static void Save<T>(string path, T value)
    {
        if (SchemaInfo.IsNewer(value)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        var bytes = new UTF8Encoding(false).GetBytes(JsonSerializer.Serialize(value, Options));
        using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush(true);   // reach the disk before Replace, so power loss cannot leave a zero-length file
        }
        if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", ignoreMetadataErrors: true);
        else File.Move(tmp, path, overwrite: true);
    }
}
