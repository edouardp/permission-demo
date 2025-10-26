using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PermissionsApi.Services;

public static partial class GroupNameValidator
{
    public const string ValidationRules = "Group name must contain only alphanumeric characters and hyphens (A-Za-z0-9-). Cannot start or end with -.";
    private static readonly ILogger Logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger(typeof(GroupNameValidator));

    [GeneratedRegex("^(?!-)(?!.*-$)[A-Za-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            Logger.LogDebug("Group name validation failed: null or empty");
            return false;
        }
        
        var isValid = ValidationRegex().IsMatch(name);
        if (!isValid)
        {
            Logger.LogDebug("Group name validation failed for: {GroupName}", name);
        }
        return isValid;
    }
}
