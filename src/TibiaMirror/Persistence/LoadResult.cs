namespace TibiaMirror.Persistence;

public enum LoadStatus { Ok, Missing, RestoredFromBackup, Corrupt }

public sealed record LoadResult<T>(T Value, LoadStatus Status, string? CorruptPath);
