namespace TibiaMirror;
public sealed class AppPaths
{
    public AppPaths(string root) { Root = root; Directory.CreateDirectory(ProfilesDir); Directory.CreateDirectory(LogsDir); }
    public static AppPaths Default() => new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppInfo.DataFolderName));
    public string Root { get; }
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string ProfilesDir => Path.Combine(Root, "profiles");
    public string LastProfileFile => Path.Combine(Root, "last_profile");
    public string LogsDir => Path.Combine(Root, "logs");
    public string ProfileFile(string name) => Path.Combine(ProfilesDir, name + ".json");
}
