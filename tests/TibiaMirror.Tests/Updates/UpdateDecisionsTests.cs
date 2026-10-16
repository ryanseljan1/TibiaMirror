using TibiaMirror.Localization;
using TibiaMirror.Updates;

namespace TibiaMirror.Tests.Updates;

public class UpdateDecisionsTests
{
    [Fact] public void Installed_build_offers_update_and_skip()
    {
        var p = UpdateDecisions.Available(portable: false);
        Assert.Equal([UpdateActionKind.WhatsNew, UpdateActionKind.UpdateNow, UpdateActionKind.Later, UpdateActionKind.Skip], p.Actions);
    }

    [Fact] public void Portable_build_offers_download_not_install()
    {
        var p = UpdateDecisions.Available(portable: true);
        Assert.DoesNotContain(UpdateActionKind.UpdateNow, p.Actions);
        Assert.DoesNotContain(UpdateActionKind.Skip, p.Actions);
        Assert.Contains(UpdateActionKind.Download, p.Actions);
        Assert.Equal("Updates_AvailablePortable", p.MessageKey);
    }

    [Fact] public void Verification_failure_has_its_own_message_and_retry()
    {
        var p = UpdateDecisions.Failure(new UpdateVerificationException("x"));
        Assert.Equal("Updates_VerifyFailed", p.MessageKey);
        Assert.Equal([UpdateActionKind.Retry, UpdateActionKind.Later], p.Actions);
        Assert.Equal("Updates_DownloadFailed", UpdateDecisions.Failure(new IOException()).MessageKey);
    }

    [Fact] public void All_keys_resolve()
    {
        foreach (var k in Enum.GetValues<UpdateActionKind>())
            Assert.NotEqual(UpdateDecisions.ActionLabelKey(k), Loc.Get(UpdateDecisions.ActionLabelKey(k)));
        foreach (var key in new[] { "Updates_Available", "Updates_AvailablePortable", "Updates_VerifyFailed", "Updates_DownloadFailed" })
            Assert.NotEqual(key, Loc.Get(key));
    }
}
