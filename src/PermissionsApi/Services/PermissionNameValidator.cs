using System.Text.RegularExpressions;

namespace PermissionsApi.Services;

public static partial class PermissionNameValidator
{
    [GeneratedRegex("^[A-Za-z0-9:-]+$", RegexOptions.Compiled)]
    private static partial Regex ValidCharactersRegex();

    [GeneratedRegex(":{2,}", RegexOptions.Compiled)]
    private static partial Regex MultipleColonsRegex();

    [GeneratedRegex("-:|:-", RegexOptions.Compiled)]
    private static partial Regex HyphenColonAdjacentRegex();

    public static bool IsValid(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        if (!ValidCharactersRegex().IsMatch(name))
            return false;

        if (name.StartsWith(':') || name.StartsWith('-'))
            return false;

        if (name.EndsWith(':') || name.EndsWith('-'))
            return false;

        if (MultipleColonsRegex().IsMatch(name))
            return false;

        if (HyphenColonAdjacentRegex().IsMatch(name))
            return false;

        return true;
    }
}
