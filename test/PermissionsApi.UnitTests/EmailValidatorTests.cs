using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class EmailValidatorTests
{
    [Theory]
    [InlineData("user@example.com")]
    [InlineData("john.doe@company.com")]
    [InlineData("admin@test.org")]
    [InlineData("support@example.co.uk")]
    [InlineData("user123@domain.com")]
    [InlineData("a@a.au")]
    public void IsValid_StandardEmails_ReturnsTrue(string email)
    {
        Assert.True(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("first.last@company.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user_name@domain.com")]
    [InlineData("user-name@domain.com")]
    public void IsValid_EmailsWithSpecialCharacters_ReturnsTrue(string email)
    {
        Assert.True(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("admin@subdomain.example.com")]
    [InlineData("user@mail.company.co.uk")]
    [InlineData("someone@branch.country.example.com")]
    public void IsValid_EmailsWithSubdomains_ReturnsTrue(string email)
    {
        Assert.True(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("")]
    public void IsValid_Empty_ReturnsFalse(string email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }

    [Fact]
    public void IsValid_Null_ReturnsFalse()
    {
        Assert.False(EmailValidator.IsValid(null!));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@domain")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void IsValid_MissingParts_ReturnsFalse(string email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("user @example.com")]
    [InlineData("user@ example.com")]
    [InlineData("user@example .com")]
    public void IsValid_WithSpaces_ReturnsFalse(string email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("user@@example.com")]
    [InlineData("user@example..com")]
    public void IsValid_InvalidCharacterSequences_ReturnsFalse(string email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }

    [Theory]
    [InlineData("user@example")]
    [InlineData("user@example.c")]
    public void IsValid_InvalidTLD_ReturnsFalse(string email)
    {
        Assert.False(EmailValidator.IsValid(email));
    }
}
