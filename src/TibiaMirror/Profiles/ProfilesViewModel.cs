using System.Collections.ObjectModel;

namespace TibiaMirror.Profiles;

public sealed record ProfileItem(string Name, bool IsCurrent)
{
    public string Display => IsCurrent ? "\u2713 " + Name : Name;
}

public sealed class ProfilesViewModel
{
    readonly ProfileService _service;
    public ProfilesViewModel(ProfileService service) { _service = service; Refresh(); }
    public ProfileService Service => _service;
    public ObservableCollection<ProfileItem> Items { get; } = new();

    public void Refresh()
    {
        Items.Clear();
        foreach (var n in _service.List())
            Items.Add(new ProfileItem(n, string.Equals(n, _service.Current.Name, StringComparison.OrdinalIgnoreCase)));
    }
}
