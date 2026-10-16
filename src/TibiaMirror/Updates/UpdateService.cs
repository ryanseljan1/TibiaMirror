using System.Net.Http;
using System.Diagnostics;
using TibiaMirror.Logging;
using TibiaMirror.Models;

namespace TibiaMirror.Updates;

public sealed class UpdateService
{
    public const string FeedUrl = "https://api.github.com/repos/ryanseljan1/TibiaMirror/releases/latest";
    public const string FeedEnvVar = "TM_UPDATE_FEED";

    readonly HttpClient _http;
    readonly Func<AppSettings> _settings;
    readonly SemVersion _current;
    readonly string? _processPath;
    readonly Action _requestShutdown;
    readonly Func<ProcessStartInfo, Process?> _startProcess;
    readonly string _workDir;

    public event EventHandler<ReleaseInfo>? UpdateAvailable;

    public UpdateService(HttpClient http, Func<AppSettings> settings, string currentVersion, string? processPath,
        Action requestShutdown, Func<ProcessStartInfo, Process?>? startProcess = null, string? workDir = null)
    {
        _http = http;
        _settings = settings;
        _current = SemVersion.Parse(currentVersion);
        _processPath = processPath;
        _requestShutdown = requestShutdown;
        _startProcess = startProcess ?? Process.Start;
        _workDir = workDir ?? Path.Combine(Path.GetTempPath(), "TibiaMirror-update");
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
            _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", $"TibiaMirror/{currentVersion}");
    }

    public bool IsPortable => IsPortablePath(_processPath);

    public static bool IsPortablePath(string? processPath)
    {
        var dir = string.IsNullOrEmpty(processPath) ? null : Path.GetDirectoryName(processPath);
        return dir is null || !File.Exists(Path.Combine(dir, "unins000.exe"));
    }

    public Task<ReleaseInfo?> CheckAsync(CancellationToken ct = default) =>
        _settings().CheckForUpdates == true ? CheckCoreAsync(honourSkip: true, ct) : Task.FromResult<ReleaseInfo?>(null);

    public Task<ReleaseInfo?> CheckNowAsync(CancellationToken ct = default) => CheckCoreAsync(honourSkip: false, ct);

    async Task<ReleaseInfo?> CheckCoreAsync(bool honourSkip, CancellationToken ct)
    {
        try
        {
            var feed = Environment.GetEnvironmentVariable(FeedEnvVar);
            var json = await ReadTextAsync(string.IsNullOrEmpty(feed) ? FeedUrl : feed, ct).ConfigureAwait(false);
            var release = ReleaseInfo.Parse(json);
            if (release is null) { Log.Warn("Update feed returned no usable release."); return null; }
            var offer = honourSkip
                ? UpdatePolicy.ShouldOffer(release.Version, _current, _settings().SkippedVersion)
                : release.Version > _current;
            if (!offer) return null;
            UpdateAvailable?.Invoke(this, release);
            return release;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { return null; }
        catch (Exception e)
        {
            Log.Warn($"Update check failed: {e.Message}");
            return null;
        }
    }

    public async Task DownloadAndInstallAsync(ReleaseInfo release, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (IsPortable) throw new InvalidOperationException("Portable builds are updated manually.");
        if (release.SumsUrl is null) throw new UpdateVerificationException("Release has no SHA256SUMS.txt.");
        Directory.CreateDirectory(_workDir);
        var path = Path.Combine(_workDir, Path.GetFileName(release.InstallerName));
        var sums = Sha256Sums.Parse(await ReadTextAsync(release.SumsUrl, ct).ConfigureAwait(false));
        if (!sums.TryGetValue(release.InstallerName, out var expected))
            throw new UpdateVerificationException($"SHA256SUMS.txt has no entry for {release.InstallerName}.");
        await DownloadAsync(release.InstallerUrl, path, progress, ct).ConfigureAwait(false);
        bool ok;
        await using (var fs = File.OpenRead(path)) ok = Sha256Sums.Verify(fs, expected);
        if (!ok)
        {
            try { File.Delete(path); } catch (IOException) { }
            throw new UpdateVerificationException($"Hash mismatch for {release.InstallerName}.");
        }
        var exe = _processPath ?? throw new InvalidOperationException("Unknown process path.");
        _startProcess(new ProcessStartInfo("cmd.exe", BuildInstallCommandLine(path, exe))
        { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden });
        _requestShutdown();
    }

    public static string BuildInstallCommandLine(string installerPath, string appExePath) =>
        $"/d /s /c \"start \"\" /wait \"{installerPath}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART & start \"\" \"{appExePath}\"\"";

    async Task<string> ReadTextAsync(string location, CancellationToken ct)
    {
        if (TryLocalPath(location, out var local)) return await File.ReadAllTextAsync(local, ct).ConfigureAwait(false);
        using var resp = await _http.GetAsync(location, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
    }

    async Task DownloadAsync(string url, string dest, IProgress<double>? progress, CancellationToken ct)
    {
        if (TryLocalPath(url, out var local))
        {
            File.Copy(local, dest, overwrite: true);
            progress?.Report(1);
            return;
        }
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength;
        await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var dst = File.Create(dest);
        var buf = new byte[81920];
        long read = 0;
        int n;
        while ((n = await src.ReadAsync(buf, ct).ConfigureAwait(false)) > 0)
        {
            await dst.WriteAsync(buf.AsMemory(0, n), ct).ConfigureAwait(false);
            read += n;
            if (total > 0) progress?.Report((double)read / total.Value);
        }
        progress?.Report(1);
    }

    static bool TryLocalPath(string location, out string path)
    {
        path = location;
        if (Uri.TryCreate(location, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile) { path = uri.LocalPath; return true; }
            return false;
        }
        return true;
    }
}
