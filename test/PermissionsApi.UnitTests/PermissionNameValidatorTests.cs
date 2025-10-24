using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class PermissionNameValidatorTests
{
    [Theory]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("delete")]
    [InlineData("a")]
    [InlineData("z")]
    public void IsValid_LowercaseLetters_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("WRITE")]
    [InlineData("Delete")]
    [InlineData("A")]
    [InlineData("Z")]
    public void IsValid_UppercaseLetters_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read123")]
    [InlineData("0")]
    [InlineData("9")]
    [InlineData("permission1")]
    [InlineData("123abc")]
    public void IsValid_Numbers_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read-write")]
    [InlineData("create-update-delete")]
    [InlineData("a-b-c")]
    public void IsValid_Hyphens_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("tenant:read")]
    [InlineData("service:api:execute")]
    [InlineData("a:b:c:d")]
    public void IsValid_Colons_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("Read-Write123")]
    [InlineData("tenant:read-write")]
    [InlineData("service:api:v2:execute")]
    [InlineData("Admin-User:Create-Update-Delete")]
    [InlineData("a1-b2:c3")]
    public void IsValid_MixedValidCharacters_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void IsValid_EmptyOrWhitespace_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read write")]
    [InlineData("a b")]
    [InlineData(" read")]
    [InlineData("write ")]
    public void IsValid_Spaces_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read@write")]
    [InlineData("user#permission")]
    [InlineData("admin$access")]
    [InlineData("test%value")]
    [InlineData("data&control")]
    public void IsValid_SpecialCharacters_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read.write")]
    [InlineData("user_permission")]
    [InlineData("admin/access")]
    [InlineData("test\\value")]
    public void IsValid_CommonInvalidCharacters_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read\nwrite")]
    [InlineData("read\twrite")]
    [InlineData("read\rwrite")]
    public void IsValid_ControlCharacters_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("café")]
    [InlineData("naïve")]
    [InlineData("résumé")]
    public void IsValid_AccentedCharacters_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("hello世界")]
    [InlineData("test日本語")]
    public void IsValid_UnicodeCharacters_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData(":read")]
    [InlineData(":write:delete")]
    [InlineData(":")]
    [InlineData(":abc")]
    public void IsValid_StartsWithColon_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read:")]
    [InlineData("write:delete:")]
    [InlineData("abc:")]
    public void IsValid_EndsWithColon_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("-read")]
    [InlineData("-write-delete")]
    [InlineData("-")]
    [InlineData("-abc")]
    public void IsValid_StartsWithHyphen_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read-")]
    [InlineData("write-delete-")]
    [InlineData("abc-")]
    public void IsValid_EndsWithHyphen_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read::write")]
    [InlineData("a::b")]
    [InlineData("service:::api")]
    [InlineData("test::::value")]
    public void IsValid_MultipleConsecutiveColons_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read--write")]
    [InlineData("a---b")]
    public void IsValid_MultipleConsecutiveHyphens_ReturnsTrue(string name)
    {
        Assert.True(PermissionNameValidator.IsValid(name));
    }

    [Theory]
    [InlineData("read:-write")]
    [InlineData("tenant-:read")]
    [InlineData("a:-b")]
    [InlineData("x-:y")]
    public void IsValid_HyphenAdjacentToColon_ReturnsFalse(string name)
    {
        Assert.False(PermissionNameValidator.IsValid(name));
    }
}
