namespace TibiaMirror.Audio;

/// <summary>File types accepted for custom timer sounds. Ogg is left out: Media Foundation has no Vorbis decoder by default.</summary>
public static class SoundFormats
{
    static readonly string[] Native = { ".mp3", ".wav" };
    static readonly string[] MediaFoundation = { ".m4a", ".aac", ".flac" };   // flac needs Windows 10+; failures fall back to the default tone

    public static IReadOnlyList<string> Extensions { get; } = Native.Concat(MediaFoundation).ToArray();

    public static bool IsSupported(string? path) =>
        !string.IsNullOrEmpty(path) && Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    public static bool UsesMediaFoundation(string path) =>
        MediaFoundation.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Filter string for an OpenFileDialog.</summary>
    public static string DialogFilter
    {
        get
        {
            var globs = string.Join(";", Extensions.Select(e => "*" + e));
            return $"Audio ({string.Join(", ", Extensions.Select(e => "*" + e))})|{globs}";
        }
    }
}
