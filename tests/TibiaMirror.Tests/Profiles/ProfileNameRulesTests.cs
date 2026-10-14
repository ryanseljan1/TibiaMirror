using TibiaMirror.Models; using TibiaMirror.Profiles;
namespace TibiaMirror.Tests.Profiles;

public class ProfileNameRulesTests
{
    static readonly string[] Existing = { "Default", "Knight" };
    [Theory]
    [InlineData("", "empty")] [InlineData("   ", "empty")]
    [InlineData("Knight/PvP", "character")] [InlineData("a:b", "character")]
    [InlineData("CON", "reserved")] [InlineData("com1", "reserved")] [InlineData("nul.txt", "reserved")]
    [InlineData("default", "exists")] [InlineData("ends.", "end")]
    public void Rejects(string name, string fragment) =>
        Assert.Contains(fragment, ProfileNameRules.Validate(name, Existing)!, StringComparison.OrdinalIgnoreCase);
    [Fact] public void Rejects_over_40_chars() => Assert.NotNull(ProfileNameRules.Validate(new string('a', 41), Existing));
    [Fact] public void Accepts_normal_name() => Assert.Null(ProfileNameRules.Validate("Paladin Hunt", Existing));
    [Fact] public void Rename_to_same_name_different_case_is_ok() => Assert.Null(ProfileNameRules.Validate("knight", Existing, renamingFrom: "Knight"));
}
