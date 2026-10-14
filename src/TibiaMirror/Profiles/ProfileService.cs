using System.Text.Json;
using TibiaMirror.Models;
using TibiaMirror.Persistence;

namespace TibiaMirror.Profiles;

public sealed class ProfileService(AppPaths paths)
{
    public Profile Current { get; private set; } = new();
    public event Action<Profile>? CurrentChanged;
    public event Action<string>? Notice;

    public void LoadInitial()
    {
        try
        {
            string? last = null;
            try { if (File.Exists(paths.LastProfileFile)) last = File.ReadAllText(paths.LastProfileFile).Trim(); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            var names = List();
            var pick = names.FirstOrDefault(n => string.Equals(n, last, StringComparison.OrdinalIgnoreCase))
                       ?? names.FirstOrDefault() ?? "Default";
            Current = LoadProfile(pick);
            JsonStore.Save(paths.ProfileFile(Current.Name), Current);   // creates Default on first run
            WriteLast();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _loadFailed = true;   // any failed startup IO leaves Current untrusted; autosave must not overwrite a healthy file
            throw;
        }
    }

    public IReadOnlyList<string> List()
    {
        if (!Directory.Exists(paths.ProfilesDir)) return Array.Empty<string>();
        return Directory.GetFiles(paths.ProfilesDir, "*.json")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
    }

    bool _loadFailed;   // startup read failed: Current is a placeholder and must not overwrite the real file

    public void SaveCurrent()
    {
        if (_loadFailed) return;
        JsonStore.Save(paths.ProfileFile(Current.Name), Current);
    }

    public Profile Create(string name)
    {
        name = ValidateNew(name);
        SaveCurrent();
        var p = new Profile { Name = name };
        JsonStore.Save(paths.ProfileFile(name), p);
        return MakeCurrent(p);
    }

    public Profile Duplicate(string source, string newName)
    {
        var src = Existing(source);
        newName = ValidateNew(newName);
        SaveCurrent();
        var loaded = LoadProfile(src);
        var copy = JsonSerializer.Deserialize<Profile>(JsonSerializer.Serialize(loaded, JsonStore.Options), JsonStore.Options)!;
        copy.Name = newName;
        copy.SchemaVersion = SchemaInfo.Current;   // the copy is a new file written by this version
        foreach (var r in copy.Regions) r.Id = Guid.NewGuid();
        foreach (var t in copy.Timers) t.Id = Guid.NewGuid();
        JsonStore.Save(paths.ProfileFile(newName), copy);
        return MakeCurrent(copy);
    }

    public void Rename(string oldName, string newName)
    {
        var old = Existing(oldName);
        newName = newName.Trim();
        var err = ProfileNameRules.Validate(newName, List(), renamingFrom: old);
        if (err != null) throw new ArgumentException(err);
        var isCurrent = string.Equals(old, Current.Name, StringComparison.OrdinalIgnoreCase);
        if (isCurrent) SaveCurrent();
        File.Move(paths.ProfileFile(old), paths.ProfileFile(newName));
        var bak = paths.ProfileFile(old) + ".bak";
        if (File.Exists(bak)) File.Move(bak, paths.ProfileFile(newName) + ".bak", overwrite: true);
        if (isCurrent)
        {
            Current.Name = newName;
            WriteLast();
            CurrentChanged?.Invoke(Current);
        }
    }

    public void Delete(string name)
    {
        var n = Existing(name);
        var names = List();
        if (names.Count <= 1) throw new InvalidOperationException("You need at least one profile.");
        var isCurrent = string.Equals(n, Current.Name, StringComparison.OrdinalIgnoreCase);
        File.Delete(paths.ProfileFile(n));
        if (File.Exists(paths.ProfileFile(n) + ".bak")) File.Delete(paths.ProfileFile(n) + ".bak");
        if (!isCurrent) return;
        var remaining = List();
        var next = remaining.FirstOrDefault(x => StringComparer.OrdinalIgnoreCase.Compare(x, n) > 0) ?? remaining[0];
        MakeCurrent(LoadProfile(next));
    }

    public void Switch(string name)
    {
        var n = Existing(name);
        if (string.Equals(n, Current.Name, StringComparison.OrdinalIgnoreCase)) return;
        SaveCurrent();
        MakeCurrent(LoadProfile(n));
    }

    public void SwitchNext()
    {
        var names = List();
        if (names.Count < 2) return;
        var i = names.ToList().FindIndex(n => string.Equals(n, Current.Name, StringComparison.OrdinalIgnoreCase));
        Switch(names[(i + 1) % names.Count]);
    }

    public string Import(string jsonPath)
    {
        Profile? p;
        try { p = JsonSerializer.Deserialize<Profile>(File.ReadAllText(jsonPath), JsonStore.Options); }
        catch (Exception e) when (e is JsonException or NotSupportedException) { p = null; }
        if (p is null) throw new ArgumentException("That file isn't a valid TibiaMirror profile.");
        if (SchemaInfo.IsNewer(p)) throw new ArgumentException("That profile was made by a newer version of TibiaMirror. Please update the app to import it.");
        ProfileNormalizer.Normalize(p);
        var name = (string.IsNullOrWhiteSpace(p.Name) ? Path.GetFileNameWithoutExtension(jsonPath) : p.Name).Trim();
        var existing = List();
        var err = ProfileNameRules.Validate(name, Array.Empty<string>());
        if (err != null) throw new ArgumentException(err);
        var final = name;
        for (var i = 2; existing.Contains(final, StringComparer.OrdinalIgnoreCase); i++)
        {
            var suffix = $" ({i})";
            final = name[..Math.Min(name.Length, ProfileNameRules.MaxLength - suffix.Length)].TrimEnd() + suffix;
        }
        err = ProfileNameRules.Validate(final, existing);
        if (err != null) throw new ArgumentException(err);
        p.Name = final;
        JsonStore.Save(paths.ProfileFile(final), p);
        return final;
    }

    public void Export(string name, string destinationPath)
    {
        var n = Existing(name);
        if (string.Equals(n, Current.Name, StringComparison.OrdinalIgnoreCase)) SaveCurrent();
        File.Copy(paths.ProfileFile(n), destinationPath, overwrite: true);
    }

    string Existing(string name) =>
        List().FirstOrDefault(n => string.Equals(n, name?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"There is no profile named '{name}'.");

    string ValidateNew(string name)
    {
        name = (name ?? "").Trim();
        var err = ProfileNameRules.Validate(name, List());
        if (err != null) throw new ArgumentException(err);
        return name;
    }

    Profile MakeCurrent(Profile p)
    {
        Current = p;
        _loadFailed = false;
        WriteLast();
        CurrentChanged?.Invoke(p);
        return p;
    }

    void WriteLast()
    {
        try { File.WriteAllText(paths.LastProfileFile, Current.Name); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { TibiaMirror.Logging.Log.Error("Could not write last_profile", e); }
    }

    Profile LoadProfile(string name)
    {
        var r = JsonStore.Load(paths.ProfileFile(name), () => new Profile { Name = name });
        r.Value.Name = name;
        ProfileNormalizer.Normalize(r.Value);
        if (SchemaInfo.IsNewer(r.Value))
            Notice?.Invoke($"Profile '{name}' was made by a newer version of TibiaMirror. You can use it, but changes to it will not be saved.");
        if (r.Status == LoadStatus.RestoredFromBackup)
            Notice?.Invoke($"Profile '{name}' was damaged and has been restored from its backup.");
        else if (r.Status == LoadStatus.Corrupt)
        {
            Notice?.Invoke($"Profile '{name}' was damaged. A fresh copy was started; the damaged file was kept as {Path.GetFileName(r.CorruptPath)}.");
            JsonStore.Save(paths.ProfileFile(name), r.Value);
        }
        return r.Value;
    }
}

