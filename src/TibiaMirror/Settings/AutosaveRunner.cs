namespace TibiaMirror.Settings;

public static class AutosaveRunner
{
    /// <summary>Runs both saves (one failing doesn't skip the other), logs failures, then rethrows so the caller's Debouncer re-arms.</summary>
    public static void SaveBoth(Action saveProfile, Action saveSettings)
    {
        var errors = new List<Exception>();
        Try(saveProfile, "Autosave of profile failed", errors);
        Try(saveSettings, "Autosave of settings failed", errors);
        if (errors.Count == 1) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1) throw new AggregateException(errors);
    }

    static void Try(Action save, string message, List<Exception> errors)
    {
        try { save(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { TibiaMirror.Logging.Log.Error(message, e); errors.Add(e); }
    }
}