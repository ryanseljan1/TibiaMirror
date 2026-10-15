namespace TibiaMirror.Settings;

/// <summary>The slice of the registry "Run" key the Start-with-Windows toggle needs, so the logic can be tested without touching it.</summary>
public interface IRunKey
{
    string? Get(string name);
    void Set(string name, string value);
    void Remove(string name);
}

/// <summary>HKCU\Software\Microsoft\Windows\CurrentVersion\Run.</summary>
public sealed class RegistryRunKey : IRunKey
{
    const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public string? Get(string name)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath);
        return k?.GetValue(name) as string;
    }

    public void Set(string name, string value)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KeyPath);
        k.SetValue(name, value);
    }

    public void Remove(string name)
    {
        using var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }
}

/// <summary>Start-with-Windows: a Run value that launches the app with <see cref="StartupArg"/> so it starts in the tray.</summary>
public sealed class StartupRegistration(IRunKey key, string exePath)
{
    public const string ValueName = "TibiaMirror";
    public const string StartupArg = "--startup";

    public static string BuildCommand(string exePath) => $"\"{exePath}\" {StartupArg}";

    public static bool IsStartupLaunch(IEnumerable<string> args) =>
        args.Any(a => string.Equals(a, StartupArg, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when a Run entry exists at all (the installer may have created it).</summary>
    public bool IsEnabled() => !string.IsNullOrWhiteSpace(key.Get(ValueName));

    public void SetEnabled(bool enabled)
    {
        if (enabled) key.Set(ValueName, BuildCommand(exePath));
        else key.Remove(ValueName);
    }

    /// <summary>Applies the toggle only when it differs from the registry, so an unchanged box never rewrites an installer-made entry.</summary>
    public void Apply(bool enabled)
    {
        if (enabled == IsEnabled()) return;
        SetEnabled(enabled);
    }
}
