using System.Collections.Concurrent;
using NAudio.Wave;
using TibiaMirror.Localization;

namespace TibiaMirror.Audio;

public static class BuiltInSounds
{
    public const string DefaultId = "builtin:chime";
    private const int SampleRate = 44100;
    private const double Peak = 0.8;
    private const double Attack = 0.005;
    private const double Release = 0.1; // short tail after the nominal note duration

    private readonly record struct Note(double Freq, double Start, double Dur, double Amp = 1.0);

    private static readonly Dictionary<string, Note[]> Recipes = new()
    {
        ["builtin:chime"] = new[] { new Note(1046.5, 0, 0.6), new Note(1568, 0.0, 0.6, 0.5) },
        ["builtin:ding"] = new[] { new Note(1318.5, 0, 0.4) },
        ["builtin:double-ding"] = new[] { new Note(1318.5, 0, 0.25), new Note(1318.5, 0.3, 0.25) },
        ["builtin:alert"] = new[] { new Note(880, 0, 0.12), new Note(880, 0.18, 0.12), new Note(880, 0.36, 0.12) },
        ["builtin:low-beep"] = new[] { new Note(440, 0, 0.35) },
        ["builtin:rise"] = new[] { new Note(523.25, 0, 0.15), new Note(659.25, 0.15, 0.15), new Note(783.99, 0.3, 0.3) },
    };

    private static readonly string[] Ids = { "builtin:chime", "builtin:ding", "builtin:double-ding", "builtin:alert", "builtin:low-beep", "builtin:rise" };
    private static readonly string[] NameKeys = { "Timers_SoundChime", "Timers_SoundDing", "Timers_SoundDoubleDing", "Timers_SoundAlert", "Timers_SoundLowBeep", "Timers_SoundRise" };
    private static IReadOnlyList<(string Id, string DisplayName)>? _all;
    private static System.Globalization.CultureInfo? _allCulture;

    /// <summary>Built-in sounds with localized names; cached per UI culture.</summary>
    public static IReadOnlyList<(string Id, string DisplayName)> All
    {
        get
        {
            var culture = LocalizationManager.Instance.Culture;
            var cached = _all;
            if (cached is not null && Equals(_allCulture, culture)) return cached;
            var list = Ids.Select((id, i) => (id, Loc.Get(NameKeys[i]))).ToArray();
            _all = list; _allCulture = culture;
            return list;
        }
    }
    private static readonly ConcurrentDictionary<string, byte[]> Cache = new();

    public static bool IsKnown(string id) => id != null && Recipes.ContainsKey(id);

    public static byte[] RenderWav(string id)
    {
        if (!IsKnown(id)) throw new ArgumentException($"Unknown built-in sound '{id}'", nameof(id));
        return Cache.GetOrAdd(id, Render);
    }

    private static byte[] Render(string id)
    {
        var notes = Recipes[id];
        double total = notes.Max(n => n.Start + n.Dur) + Release;
        var buf = new double[(int)(total * SampleRate)];
        foreach (var n in notes)
        {
            int s0 = (int)(n.Start * SampleRate);
            int count = (int)((n.Dur + Release) * SampleRate);
            for (int i = 0; i < count && s0 + i < buf.Length; i++)
            {
                double t = (double)i / SampleRate;
                double env = Math.Exp(-t * 6) * Math.Min(1.0, t / Attack);
                buf[s0 + i] += n.Amp * env * Math.Sin(2 * Math.PI * n.Freq * t);
            }
        }
        double max = buf.Max(Math.Abs);
        double scale = max > 0 ? Peak / max : 1;
        using var ms = new MemoryStream();
        using (var w = new WaveFileWriter(ms, new WaveFormat(SampleRate, 16, 1)))
        {
            foreach (var v in buf) w.WriteSample((float)(v * scale));
        }
        return ms.ToArray();
    }
}
