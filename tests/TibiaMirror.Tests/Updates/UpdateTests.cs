using System.Net.Http;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using TibiaMirror.Models;
using TibiaMirror.Updates;
using Xunit;

namespace TibiaMirror.Tests.Updates;

public sealed class UpdateTests : IDisposable
{
    readonly string _dir = Path.Combine(Path.GetTempPath(), "tm-upd-" + Guid.NewGuid().ToString("N"));
    public UpdateTests() => Directory.CreateDirectory(_dir);
    public void Dispose()
    {
        Environment.SetEnvironmentVariable(UpdateService.FeedEnvVar, null);
        try { Directory.Delete(_dir, true); } catch (IOException) { }
    }

    [Theory]
    [InlineData("1.2.3", "1.2.4", -1)]
    [InlineData("v1.10.0", "1.9.9", 1)]
    [InlineData("0.2.0-beta.1", "0.2.0", -1)]
    [InlineData("0.2.0-beta.2", "0.2.0-beta.10", -1)]
    [InlineData("v0.2.0", "0.2.0", 0)]
    public void Compare(string a, string b, int expected) =>
        Assert.Equal(expected, Math.Sign(SemVersion.Parse(a).CompareTo(SemVersion.Parse(b))));

    [Fact] public void ParseRejectsGarbage() => Assert.Null(SemVersion.TryParse("latest"));

    static string Json(string tag, string sums = "https://x/SHA256SUMS.txt", string inst = "https://x/i.exe")
    {
        var v = tag.TrimStart('v');
        return "{\"tag_name\":\"" + tag + "\",\"html_url\":\"https://x/r\",\"body\":\"notes\",\"assets\":["
            + "{\"name\":\"TibiaMirror-Setup.exe\",\"browser_download_url\":\"https://x/generic.exe\"},"
            + "{\"name\":\"TibiaMirror-Setup-" + v + ".exe\",\"browser_download_url\":\"" + inst.Replace("\\", "\\\\") + "\"},"
            + "{\"name\":\"SHA256SUMS.txt\",\"browser_download_url\":\"" + sums.Replace("\\", "\\\\") + "\"}]}";
    }

    [Fact]
    public void ParsesRelease()
    {
        var r = ReleaseInfo.Parse(Json("v0.2.0"))!;
        Assert.Equal("0.2.0", r.Version.ToString());
        Assert.Equal("https://x/i.exe", r.InstallerUrl);
        Assert.Equal("https://x/SHA256SUMS.txt", r.SumsUrl);
        Assert.Equal("notes", r.Body);
        Assert.Null(ReleaseInfo.Parse("not json"));
    }

    [Fact]
    public void ParsesSums()
    {
        var h = new string('a', 64);
        var m = Sha256Sums.Parse($"{h}  TibiaMirror-Setup-0.2.0.exe\r\n{h.ToUpperInvariant()} *b.zip\n");
        Assert.Equal(h, m["TibiaMirror-Setup-0.2.0.exe"]);
        Assert.True(m.ContainsKey("b.zip"));
    }

    [Fact]
    public void HashVerify()
    {
        var data = Encoding.UTF8.GetBytes("hello");
        var hex = Convert.ToHexString(SHA256.HashData(data));
        Assert.True(Sha256Sums.Verify(new MemoryStream(data), hex));
        Assert.True(Sha256Sums.Verify(new MemoryStream(data), hex.ToLowerInvariant()));
        Assert.False(Sha256Sums.Verify(new MemoryStream(data), new string('0', 64)));
    }

    [Fact]
    public void Policy()
    {
        var cur = SemVersion.Parse("0.1.1");
        Assert.True(UpdatePolicy.ShouldOffer(SemVersion.Parse("0.2.0"), cur, null));
        Assert.False(UpdatePolicy.ShouldOffer(SemVersion.Parse("0.1.1"), cur, null));
        Assert.False(UpdatePolicy.ShouldOffer(SemVersion.Parse("0.2.0"), cur, "v0.2.0"));
        Assert.True(UpdatePolicy.ShouldOffer(SemVersion.Parse("0.3.0"), cur, "0.2.0"));
    }

    sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> f) : HttpMessageHandler
    {
        public int Calls;
        public HttpRequestMessage? Last;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        { Calls++; Last = r; return Task.FromResult(f(r)); }
    }

    static HttpResponseMessage Ok(string s) => new(HttpStatusCode.OK) { Content = new StringContent(s) };

    UpdateService Make(HttpMessageHandler h, AppSettings s, Action? shutdown = null, Func<ProcessStartInfo, Process?>? start = null, string exe = @"C:\x\TibiaMirror.exe") =>
        new(new HttpClient(h), () => s, "0.1.1", exe, shutdown ?? (() => { }), start ?? (_ => null), Path.Combine(_dir, "work"));

    /// <summary>An exe path with an Inno uninstaller next to it (an installed, non-portable layout).</summary>
    string Installed()
    {
        var app = Directory.CreateDirectory(Path.Combine(_dir, "app")).FullName;
        File.WriteAllText(Path.Combine(app, "unins000.exe"), "");
        return Path.Combine(app, "TibiaMirror.exe");
    }

    [Fact]
    public async Task PortableRefusesToInstall()
    {
        var h = new FakeHandler(_ => throw new InvalidOperationException("no network"));
        var rel = ReleaseInfo.Parse(Json("v0.2.0", "https://x/sums", "https://x/inst"))!;
        var started = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Make(h, new AppSettings(), start: _ => { started = true; return null; }).DownloadAndInstallAsync(rel));
        Assert.False(started);
        Assert.Equal(0, h.Calls);
    }

    [Fact]
    public async Task OptInNullOrFalseDoesNotCheck()
    {
        var h = new FakeHandler(_ => Ok(Json("v0.2.0")));
        Assert.Null(await Make(h, new AppSettings { CheckForUpdates = null }).CheckAsync());
        Assert.Null(await Make(h, new AppSettings { CheckForUpdates = false }).CheckAsync());
        Assert.Equal(0, h.Calls);
    }

    [Fact]
    public async Task CheckHonoursSkipAndCheckNowIgnoresIt()
    {
        var h = new FakeHandler(_ => Ok(Json("v0.2.0")));
        var s = new AppSettings { CheckForUpdates = true, SkippedVersion = "0.2.0" };
        var svc = Make(h, s);
        Assert.Null(await svc.CheckAsync());
        Assert.NotNull(await svc.CheckNowAsync());
        s.SkippedVersion = null;
        ReleaseInfo? raised = null;
        svc.UpdateAvailable += (_, r) => raised = r;
        Assert.NotNull(await svc.CheckAsync());
        Assert.NotNull(raised);
        Assert.Contains("TibiaMirror/", h.Last!.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task CheckNowIgnoresOptIn()
    {
        var h = new FakeHandler(_ => Ok(Json("v0.2.0")));
        Assert.NotNull(await Make(h, new AppSettings()).CheckNowAsync());
    }

    [Fact]
    public async Task NetworkFailureReturnsNull()
    {
        var h = new FakeHandler(_ => throw new HttpRequestException("down"));
        Assert.Null(await Make(h, new AppSettings { CheckForUpdates = true }).CheckAsync());
        var h2 = new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));
        Assert.Null(await Make(h2, new AppSettings { CheckForUpdates = true }).CheckNowAsync());
    }

    [Fact]
    public void PortableDetection()
    {
        var exe = Path.Combine(_dir, "TibiaMirror.exe");
        Assert.True(UpdateService.IsPortablePath(exe));
        File.WriteAllText(Path.Combine(_dir, "unins000.exe"), "");
        Assert.False(UpdateService.IsPortablePath(exe));
        Assert.True(Make(new FakeHandler(_ => Ok("")), new AppSettings(), exe: Path.Combine(_dir, "sub", "a.exe")).IsPortable);
    }

    [Fact]
    public async Task FeedOverrideViaFilePath()
    {
        var feed = Path.Combine(_dir, "feed.json");
        File.WriteAllText(feed, Json("v0.3.0"));
        Environment.SetEnvironmentVariable(UpdateService.FeedEnvVar, feed);
        var h = new FakeHandler(_ => throw new InvalidOperationException("no network"));
        var r = await Make(h, new AppSettings()).CheckNowAsync();
        Assert.Equal("0.3.0", r!.Version.ToString());
        Assert.Equal(0, h.Calls);
    }

    [Fact]
    public void CommandLineQuotesPaths()
    {
        var c = UpdateService.BuildInstallCommandLine(@"C:\Temp dir\Setup.exe", @"C:\Program Files\TibiaMirror\TibiaMirror.exe");
        Assert.Equal("/d /s /c \"start \"\" /wait \"C:\\Temp dir\\Setup.exe\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART & start \"\" \"C:\\Program Files\\TibiaMirror\\TibiaMirror.exe\"\"", c);
    }

    [Fact]
    public async Task DownloadVerifiesAndInstalls()
    {
        var bytes = Encoding.UTF8.GetBytes("installer-bytes");
        var hex = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var h = new FakeHandler(r => r.RequestUri!.AbsolutePath.EndsWith("sums") ? Ok($"{hex}  TibiaMirror-Setup-0.2.0.exe\n") : new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        var rel = ReleaseInfo.Parse(Json("v0.2.0", "https://x/sums", "https://x/inst"))!;
        ProcessStartInfo? psi = null;
        var down = false;
        await Make(h, new AppSettings(), () => down = true, p => { psi = p; return null; }, Installed()).DownloadAndInstallAsync(rel);
        Assert.True(down);
        Assert.Equal("cmd.exe", psi!.FileName);
        Assert.Contains("/VERYSILENT", psi.Arguments);
        Assert.True(File.Exists(Path.Combine(_dir, "work", "TibiaMirror-Setup-0.2.0.exe")));
    }

    [Fact]
    public async Task HashMismatchDeletesFileAndThrows()
    {
        var h = new FakeHandler(r => r.RequestUri!.AbsolutePath.EndsWith("sums") ? Ok($"{new string('0', 64)}  TibiaMirror-Setup-0.2.0.exe\n") : new(HttpStatusCode.OK) { Content = new ByteArrayContent([1, 2, 3]) });
        var rel = ReleaseInfo.Parse(Json("v0.2.0", "https://x/sums", "https://x/inst"))!;
        var started = false;
        await Assert.ThrowsAsync<UpdateVerificationException>(() => Make(h, new AppSettings(), start: _ => { started = true; return null; }, exe: Installed()).DownloadAndInstallAsync(rel));
        Assert.False(File.Exists(Path.Combine(_dir, "work", "TibiaMirror-Setup-0.2.0.exe")));
        Assert.False(started);
    }

    [Fact]
    public async Task DownloadFromFileUris()
    {
        var bytes = new byte[] { 9, 8, 7 };
        var inst = Path.Combine(_dir, "TibiaMirror-Setup-0.2.0.exe");
        File.WriteAllBytes(inst, bytes);
        var sums = Path.Combine(_dir, "SHA256SUMS.txt");
        File.WriteAllText(sums, $"{Convert.ToHexString(SHA256.HashData(bytes))}  TibiaMirror-Setup-0.2.0.exe\n");
        var rel = ReleaseInfo.Parse(Json("v0.2.0", new Uri(sums).AbsoluteUri, new Uri(inst).AbsoluteUri))!;
        var down = false;
        await Make(new FakeHandler(_ => throw new InvalidOperationException()), new AppSettings(), () => down = true, exe: Installed()).DownloadAndInstallAsync(rel);
        Assert.True(down);
    }
}
