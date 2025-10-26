using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PermissionsApi.Services;

public static partial class EmailValidator
{
    public const string ValidationRules = "Email must be a valid format (e.g., user@company.com).";
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(EmailValidator));

    [GeneratedRegex("^[A-Za-z0-9._%+-]+@(?!.*\\.\\.)([A-Za-z0-9.-]+)\\.([A-Za-z]{2,})$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            Logger.LogDebug("Email validation failed: null or empty");
            return false;
        }
        
        var isValid = ValidationRegex().IsMatch(email);
        if (!isValid)
        {
            Logger.LogDebug("Email validation failed for: {Email}", email);
        }
        return isValid;
    }
}
