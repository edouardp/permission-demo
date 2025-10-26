using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PermissionsApi.Services;

public static partial class PermissionNameValidator
{
    public const string ValidationRules = "Permission name must contain only alphanumeric characters, hyphens, and colons (A-Za-z0-9:-). Cannot start or end with : or -. Cannot contain consecutive colons. Cannot have - adjacent to :.";
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(PermissionNameValidator));

    [GeneratedRegex("^(?![-:])(?!.*::)(?!.*-:)(?!.*:-)(?!.*[-:]$)[A-Za-z0-9:-]+$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Logger.LogDebug("Permission name validation failed: null or empty");
            return false;
        }
        
        var isValid = ValidationRegex().IsMatch(name);
        if (!isValid)
        {
            Logger.LogDebug("Permission name validation failed for: {PermissionName}", name);
        }
        return isValid;
    }
}
