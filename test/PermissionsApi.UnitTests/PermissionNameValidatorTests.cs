using AwesomeAssertions;
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
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("WRITE")]
    [InlineData("Delete")]
    [InlineData("A")]
    [InlineData("Z")]
    public void IsValid_UppercaseLetters_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("read123")]
    [InlineData("write456")]
    [InlineData("delete0")]
    [InlineData("a1")]
    [InlineData("z9")]
    public void IsValid_AlphanumericMixed_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("user:read")]
    [InlineData("admin:write")]
    [InlineData("system:delete")]
    [InlineData("a:b")]
    [InlineData("x:y:z")]
    public void IsValid_WithColons_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("user-read")]
    [InlineData("admin-write")]
    [InlineData("system-delete")]
    [InlineData("a-b")]
    [InlineData("x-y-z")]
    public void IsValid_WithHyphens_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData("user:read-write")]
    [InlineData("admin:write-delete")]
    [InlineData("system:a-b:c-d")]
    public void IsValid_WithColonsAndHyphens_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData(":read")]
    [InlineData(":write")]
    [InlineData(":")]
    public void IsValid_StartsWithColon_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("read:")]
    [InlineData("write:")]
    [InlineData("a:")]
    public void IsValid_EndsWithColon_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("-read")]
    [InlineData("-write")]
    [InlineData("-")]
    public void IsValid_StartsWithHyphen_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("read-")]
    [InlineData("write-")]
    [InlineData("a-")]
    public void IsValid_EndsWithHyphen_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("user::read")]
    [InlineData("admin::write")]
    [InlineData("a::b")]
    public void IsValid_ConsecutiveColons_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("user:-read")]
    [InlineData("admin:-write")]
    [InlineData("a:-b")]
    public void IsValid_ColonFollowedByHyphen_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("user-:read")]
    [InlineData("admin-:write")]
    [InlineData("a-:b")]
    public void IsValid_HyphenFollowedByColon_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("read write")]
    [InlineData("user@read")]
    [InlineData("admin.write")]
    [InlineData("system_delete")]
    public void IsValid_InvalidCharacters_ReturnsFalse(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeFalse();
    }

    [Theory]
    [InlineData("user:read-write:execute")]
    [InlineData("admin:create-update-delete:all")]
    [InlineData("system:a1-b2:c3-d4")]
    public void IsValid_ComplexValidNames_ReturnsTrue(string name)
    {
        PermissionNameValidator.IsValid(name).Should().BeTrue();
    }
}
