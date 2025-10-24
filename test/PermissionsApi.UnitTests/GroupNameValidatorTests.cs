using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class GroupNameValidatorTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("users")]
    [InlineData("a")]
    [InlineData("z")]
    public void IsValid_LowercaseLetters_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("USERS")]
    [InlineData("A")]
    [InlineData("Z")]
    public void IsValid_UppercaseLetters_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("group123")]
    [InlineData("0")]
    [InlineData("9")]
    [InlineData("admin1")]
    [InlineData("123users")]
    public void IsValid_Numbers_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("admin-users")]
    [InlineData("my-group")]
    [InlineData("a-b-c")]
    public void IsValid_Hyphens_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("Admin-Users-123")]
    [InlineData("my-group-1")]
    [InlineData("Test123")]
    public void IsValid_MixedValidCharacters_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("")]
    public void IsValid_Empty_ReturnsFalse(string name)
    {
        Assert.False(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("-admin")]
    [InlineData("-users")]
    [InlineData("-")]
    public void IsValid_StartsWithHyphen_ReturnsFalse(string name)
    {
        Assert.False(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("admin-")]
    [InlineData("users-")]
    public void IsValid_EndsWithHyphen_ReturnsFalse(string name)
    {
        Assert.False(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("admin users")]
    [InlineData("my group")]
    [InlineData(" admin")]
    [InlineData("users ")]
    public void IsValid_Spaces_ReturnsFalse(string name)
    {
        Assert.False(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("admin:users")]
    [InlineData("my_group")]
    [InlineData("admin@group")]
    [InlineData("test.group")]
    public void IsValid_InvalidCharacters_ReturnsFalse(string name)
    {
        Assert.False(GroupNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("admin--users")]
    [InlineData("my---group")]
    public void IsValid_MultipleConsecutiveHyphens_ReturnsTrue(string name)
    {
        Assert.True(GroupNameValidator.IsValid(name));
    }
}
