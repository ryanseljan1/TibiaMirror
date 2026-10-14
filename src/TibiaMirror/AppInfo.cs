namespace TibiaMirror;
public static class AppInfo
{
    public const string Name = "TibiaMirror";
    public const string DataFolderName = "TibiaMirror";
    public static string Version => typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
}
