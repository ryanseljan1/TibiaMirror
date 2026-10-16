using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Resources;
using TibiaMirror.Logging;

namespace TibiaMirror.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    readonly Dictionary<string, ResourceManager> _managers = new();
    readonly HashSet<string> _missing = new();
    CultureInfo _culture = CultureInfo.CurrentUICulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public CultureInfo Culture => _culture;

    public string this[string key] => Lookup(key);

    public void SetCulture(CultureInfo culture)
    {
        _culture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }

    public static string BaseNameFor(string area) => $"TibiaMirror.Resources.Strings.{area}";

    string Lookup(string key)
    {
        var i = key.IndexOf('_');
        if (i > 0)
        {
            var area = key[..i];
            try
            {
                var value = Manager(area).GetString(key, _culture);
                if (value != null) return value;
            }
            catch (MissingManifestResourceException) { }
        }
        lock (_missing)
        {
            if (_missing.Add(key)) Log.Warn($"Missing localization key '{key}'");
        }
        return key;
    }

    ResourceManager Manager(string area)
    {
        lock (_managers)
        {
            if (!_managers.TryGetValue(area, out var rm))
                _managers[area] = rm = new ResourceManager(BaseNameFor(area), typeof(LocalizationManager).Assembly);
            return rm;
        }
    }
}
