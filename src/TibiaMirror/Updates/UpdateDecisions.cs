namespace TibiaMirror.Updates;

public enum UpdateActionKind { WhatsNew, UpdateNow, Download, Later, Skip, Retry }

/// <summary>What the update snackbar says and offers; pure, so it can be unit-tested without WPF.</summary>
public sealed record UpdatePrompt(string MessageKey, IReadOnlyList<UpdateActionKind> Actions);

public static class UpdateDecisions
{
    public static UpdatePrompt Available(bool portable) => portable
        ? new("Updates_AvailablePortable", [UpdateActionKind.WhatsNew, UpdateActionKind.Download, UpdateActionKind.Later])
        : new("Updates_Available", [UpdateActionKind.WhatsNew, UpdateActionKind.UpdateNow, UpdateActionKind.Later, UpdateActionKind.Skip]);

    /// <summary>While a download/install runs, new offers and Update now/Retry clicks are ignored.</summary>
    public static bool CanStartInstall(bool installing) => !installing;
    public static bool ShouldShowAvailable(bool installing) => !installing;

    /// <summary>The progress snackbar must be shown again if it was replaced or dismissed meanwhile.</summary>
    public static bool NeedsProgressReshow(bool visible, bool busy, string current, string expected) =>
        !visible || !busy || current != expected;

    public static UpdatePrompt Failure(Exception ex) => new(
        ex is UpdateVerificationException ? "Updates_VerifyFailed" : "Updates_DownloadFailed",
        [UpdateActionKind.Retry, UpdateActionKind.Later]);

    public static string ActionLabelKey(UpdateActionKind k) => k switch
    {
        UpdateActionKind.WhatsNew => "Updates_WhatsNew",
        UpdateActionKind.UpdateNow => "Updates_UpdateNow",
        UpdateActionKind.Download => "Updates_Download",
        UpdateActionKind.Later => "Updates_Later",
        UpdateActionKind.Skip => "Updates_SkipVersion",
        _ => "Updates_Retry",
    };
}
