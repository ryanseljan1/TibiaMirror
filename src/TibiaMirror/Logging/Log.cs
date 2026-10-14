using System.Text;

namespace TibiaMirror.Logging;

public static class Log
{
    static readonly object Gate = new();
    static string? _dir;

    public static void Init(string logsDir)
    {
        try
        {
            lock (Gate)
            {
                _dir = logsDir;
                Directory.CreateDirectory(logsDir);
                var cutoff = DateTime.Now.AddDays(-14);
                foreach (var f in Directory.EnumerateFiles(logsDir, "TibiaMirror-*.log"))
                {
                    try { if (File.GetLastWriteTime(f) < cutoff) File.Delete(f); } catch { }
                }
            }
        }
        catch { }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : message + Environment.NewLine + ex);

    static void Write(string level, string message)
    {
        try
        {
            lock (Gate)
            {
                if (_dir is null) return;
                var now = DateTime.Now;
                var path = Path.Combine(_dir, $"TibiaMirror-{now:yyyyMMdd}.log");
                File.AppendAllText(path, $"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch { }
    }
}
