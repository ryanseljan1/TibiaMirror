namespace TibiaMirror.Tests.Settings;

/// <summary>
/// WPF test classes run one at a time: parallel STA threads race on the first pack:// URI registration
/// ("The URI prefix is not recognized"), and only one Application may exist per process.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfCollection
{
    public const string Name = "WPF";
}
