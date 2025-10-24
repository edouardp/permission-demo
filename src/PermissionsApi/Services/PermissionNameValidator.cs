using System.Text.RegularExpressions;

namespace PermissionsApi.Services;

public static partial class PermissionNameValidator
{
    public const string ValidationRules = "Permission name must contain only alphanumeric characters, hyphens, and colons (A-Za-z0-9:-). Cannot start or end with : or -. Cannot contain consecutive colons. Cannot have - adjacent to :.";

    [GeneratedRegex("^(?![-:])(?!.*::)(?!.*-:)(?!.*:-)(?!.*[-:]$)[A-Za-z0-9:-]+$", RegexOptions.Compiled)]
    private static partial Regex ValidationRegex();

    public static bool IsValid(string name) => !string.IsNullOrEmpty(name) && ValidationRegex().IsMatch(name);
}
