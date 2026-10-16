using System.Security.Cryptography;

namespace TibiaMirror.Updates;

public sealed class UpdateVerificationException(string message) : Exception(message);

public static class Sha256Sums
{
    public static Dictionary<string, string> Parse(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length < 66) continue;
            var hash = line[..64];
            if (!hash.All(Uri.IsHexDigit)) continue;
            var name = line[64..].TrimStart(' ', '\t').TrimStart('*');
            if (name.Length > 0) map[name] = hash.ToLowerInvariant();
        }
        return map;
    }

    public static string Compute(Stream stream) => Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();

    public static bool Verify(Stream stream, string expectedHex) =>
        string.Equals(Compute(stream), expectedHex.Trim(), StringComparison.OrdinalIgnoreCase);
}
