using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace TibiaMirror.Localization;

[MarkupExtensionReturnType(typeof(BindingExpression))]
public sealed class Loc : MarkupExtension
{
    public Loc() { }
    public Loc(string key) { Key = key; }

    [ConstructorArgument("key")]
    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }

    public static string Get(string key) => LocalizationManager.Instance[key];

    public static string Format(string key, params object?[] args) =>
        string.Format(LocalizationManager.Instance.Culture, Get(key), args);
}
