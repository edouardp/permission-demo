using System.Text.RegularExpressions;

namespace PermissionsApi.Services;

public static partial class GroupNameValidator
{
    public const string ValidationRules = "Group name must contain only alphanumeric characters and hyphens (A-Za-z0-9-). Cannot start or end with -.";

    [GeneratedRegex("^(?!-)(?!.*-$)[A-Za-z0-9-]+$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string name) => !string.IsNullOrEmpty(name) && ValidationRegex().IsMatch(name);
}
