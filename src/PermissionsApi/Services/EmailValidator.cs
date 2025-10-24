using System.Text.RegularExpressions;

namespace PermissionsApi.Services;

public static partial class EmailValidator
{
    public const string ValidationRules = "Email must be a valid format (e.g., user@company.com).";

    [GeneratedRegex("^[A-Za-z0-9._%+-]+@(?!.*\\.\\.)([A-Za-z0-9.-]+)\\.([A-Za-z]{2,})$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string email) => !string.IsNullOrEmpty(email) && ValidationRegex().IsMatch(email);
}
