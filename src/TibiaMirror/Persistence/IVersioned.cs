namespace TibiaMirror.Persistence;

/// <summary>A persisted model with a schema version. Files newer than <see cref="SchemaInfo.Current"/> are loaded read-only.</summary>
public interface IVersioned { int SchemaVersion { get; } }

public static class SchemaInfo
{
    public const int Current = 1;
    public static bool IsNewer(object? value) => value is IVersioned v && v.SchemaVersion > Current;
}
