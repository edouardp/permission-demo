using AwesomeAssertions;
using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class GroupNameValidatorTests
{
    [Theory]
    [InlineData("editors")]
    [InlineData("admins")]
    [InlineData("users")]
    [InlineData("a")]
    [InlineData("z")]
    public void IsValid_LowercaseLetters_ReturnsTrue(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("Editors")]
    [InlineData("ADMINS")]
    [InlineData("Users")]
    [InlineData("A")]
    [InlineData("Z")]
    public void IsValid_UppercaseLetters_ReturnsTrue(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("group123")]
    [InlineData("team456")]
    [InlineData("dept0")]
    [InlineData("a1")]
    [InlineData("z9")]
    public void IsValid_AlphanumericMixed_ReturnsTrue(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("content-editors")]
    [InlineData("super-admins")]
    [InlineData("power-users")]
    [InlineData("a-b")]
    [InlineData("x-y-z")]
    public void IsValid_WithHyphens_ReturnsTrue(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("-editors")]
    [InlineData("-admins")]
    [InlineData("-")]
    public void IsValid_StartsWithHyphen_ReturnsFalse(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("editors-")]
    [InlineData("admins-")]
    [InlineData("a-")]
    public void IsValid_EndsWithHyphen_ReturnsFalse(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("content editors")]
    [InlineData("super@admins")]
    [InlineData("power.users")]
    [InlineData("team_members")]
    [InlineData("group:name")]
    public void IsValid_InvalidCharacters_ReturnsFalse(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("content-editors-team")]
    [InlineData("super-power-admins")]
    [InlineData("a1-b2-c3")]
    public void IsValid_ComplexValidNames_ReturnsTrue(string name)
    {
        GroupNameValidator.IsValid(name).Should().BeTrue();
    }
}
