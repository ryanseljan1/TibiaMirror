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

    public static LoadResult<T> Load<T>(string path, Func<T> createDefault) where T : class
    {
        if (!File.Exists(path))
            return File.Exists(path + ".bak") && TryRead<T>(path + ".bak", out var b)
                ? new(b!, LoadStatus.RestoredFromBackup, null)
                : new(createDefault(), LoadStatus.Missing, null);
        if (TryRead<T>(path, out var v)) return new(v!, LoadStatus.Ok, null);
        var corrupt = path + "." + DateTime.Now.ToString("yyyyMMddHHmmss") + ".corrupt";
        try { File.Move(path, corrupt, overwrite: true); } catch (IOException) { corrupt = path; }
        if (File.Exists(path + ".bak") && TryRead<T>(path + ".bak", out var bak))
            return new(bak!, LoadStatus.RestoredFromBackup, corrupt);
        return new(createDefault(), LoadStatus.Corrupt, corrupt);
    }

    static bool TryRead<T>(string path, out T? value) where T : class
    {
        try { value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Options); return value is not null; }
        catch (Exception e) when (e is JsonException or IOException or NotSupportedException) { value = null; return false; }
    }

    public static void Save<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Options));
        if (File.Exists(path)) File.Replace(tmp, path, path + ".bak", ignoreMetadataErrors: true);
        else File.Move(tmp, path, overwrite: true);
    }
}
