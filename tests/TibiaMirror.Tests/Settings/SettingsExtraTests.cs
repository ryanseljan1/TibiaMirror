using TibiaMirror.Models; using TibiaMirror.Settings; using TibiaMirror.Persistence;
namespace TibiaMirror.Tests.Settings;

public class SettingsExtraTests : IDisposable
{
    readonly string _root = Directory.CreateTempSubdirectory("tm-set2").FullName;
    public void Dispose() => Directory.Delete(_root, true);

    [Fact] public void Corrupt_file_without_backup_reports_Corrupt_and_defaults()
    {
        var paths = new AppPaths(_root); File.WriteAllText(paths.SettingsFile, "{ not json");
        var s = new SettingsService(paths); s.Load();
        Assert.Equal(LoadStatus.Corrupt, s.LastLoadStatus); Assert.Equal("client", s.Current.TibiaProcessName); Assert.NotNull(s.CorruptPath);
    }

    [Fact] public void Corrupt_file_with_backup_reports_RestoredFromBackup()
    {
        var paths = new AppPaths(_root);
        var good = new SettingsService(paths); good.Load(); good.Current.TibiaProcessName = "mytibia"; good.Save();
        good.Current.TibiaProcessName = "mytibia2"; good.Save();           // second save creates the .bak of the first
        File.WriteAllText(paths.SettingsFile, "garbage");
        var s = new SettingsService(paths); s.Load();
        Assert.Equal(LoadStatus.RestoredFromBackup, s.LastLoadStatus); Assert.Equal("mytibia", s.Current.TibiaProcessName);
    }

    [Theory]
    [InlineData("", "Tibia", true)] [InlineData("client", "  ", true)] [InlineData(null, "Tibia", true)]
    [InlineData("client", "Tibia", false)]
    public void ValidateDetection_requires_both_fields(string? proc, string? title, bool hasError) =>
        Assert.Equal(hasError, SettingsWindow.ValidateDetection(proc, title) is not null);

    [Fact] public void RestoreClamp_Clamp_moves_rect_from_missing_monitor_to_primary()
    {
        var primary = new PixelRect(0, 0, 1920, 1040);
        var r = RestoreClamp.Clamp(new PixelRect(3000, 200, 300, 200), new[] { primary }, primary);
        Assert.True(r.X >= 0 && r.Right <= 1920 && r.Y >= 0 && r.Bottom <= 1040);
        Assert.Equal(new PixelRect(100, 100, 300, 200), RestoreClamp.Clamp(new PixelRect(100, 100, 300, 200), new[] { primary }, primary));
    }
}

public class AutosaveRetryTests
{
    [Fact] public void Failed_save_is_rearmed_and_succeeds_on_Flush()
    {
        int profileCalls = 0, settingsCalls = 0; bool fail = true;
        using var d = new Debouncer(TimeSpan.FromHours(1), () => AutosaveRunner.SaveBoth(
            () => { profileCalls++; if (fail) throw new IOException("disk"); },
            () => settingsCalls++));
        d.Trigger();
        d.Flush();                       // fails: must not throw, must re-arm
        Assert.Equal(1, profileCalls); Assert.Equal(1, settingsCalls);   // settings still saved despite profile failure
        fail = false;
        d.Flush();                       // retry
        Assert.Equal(2, profileCalls);
        d.Flush();                       // nothing pending now
        Assert.Equal(2, profileCalls);
    }

    [Fact] public void SaveBoth_throws_on_failure_and_ignores_non_io_exceptions_filter()
    {
        Assert.Throws<IOException>(() => AutosaveRunner.SaveBoth(() => throw new IOException(), () => { }));
        Assert.Throws<AggregateException>(() => AutosaveRunner.SaveBoth(() => throw new IOException(), () => throw new UnauthorizedAccessException()));
        Assert.Throws<InvalidOperationException>(() => AutosaveRunner.SaveBoth(() => throw new InvalidOperationException(), () => { }));
    }
}
