using AwesomeAssertions;
using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("admin@company.org")]
    [InlineData("test@domain.net")]
    [InlineData("a@b.co")]
    public void IsValid_StandardEmails_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user.name@example.com")]
    [InlineData("first.last@company.org")]
    [InlineData("a.b.c@domain.net")]
    public void IsValid_WithDots_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user+tag@example.com")]
    [InlineData("admin+test@company.org")]
    public void IsValid_WithPlus_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user_name@example.com")]
    [InlineData("admin_test@company.org")]
    public void IsValid_WithUnderscore_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user-name@example.com")]
    [InlineData("admin-test@company.org")]
    public void IsValid_WithHyphen_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user@sub.example.com")]
    [InlineData("admin@mail.company.org")]
    [InlineData("test@a.b.c.com")]
    public void IsValid_WithSubdomains_ReturnsTrue(string email)
    {
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("notanemail")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("user")]
    public void IsValid_InvalidFormat_ReturnsFalse(string email)
    {
        EmailValidator.IsValid(email).Should().BeFalse();
    }

    [Theory]
    [InlineData("user@example")]
    [InlineData("admin@company")]
    public void IsValid_NoTLD_ReturnsFalse(string email)
    {
        EmailValidator.IsValid(email).Should().BeFalse();
    }

    [Theory]
    [InlineData("admin@example..com")]
    public void IsValid_ConsecutiveDotsInDomain_ReturnsFalse(string email)
    {
        EmailValidator.IsValid(email).Should().BeFalse();
    }
    
    [Theory]
    [InlineData("user..name@example.com")]
    public void IsValid_ConsecutiveDotsInLocalPart_CurrentlyAllowed(string email)
    {
        // Note: Current validator allows consecutive dots in local part
        // This may not be RFC compliant but matches existing behavior
        EmailValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("user@example.c")]
    public void IsValid_TLDTooShort_ReturnsFalse(string email)
    {
        EmailValidator.IsValid(email).Should().BeFalse();
    }
}
