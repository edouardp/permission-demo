namespace PermissionsApi.Models;

public class PermissionDebugResponse
{
    public string Email { get; set; } = string.Empty;
    public List<PermissionDebugItem> Permissions { get; set; } = new();
}

public class PermissionDebugItem
{
    public string Permission { get; set; } = string.Empty;
    public string FinalResult { get; set; } = string.Empty; // "ALLOW" or "DENY"
    public List<PermissionDebugStep> Chain { get; set; } = new();
}

public class PermissionDebugStep
{
    public string Level { get; set; } = string.Empty; // "Default", "Group", "User"
    public string Source { get; set; } = string.Empty; // group name or "default" or user email
    public string Action { get; set; } = string.Empty; // "ALLOW", "DENY", or "NONE"
}
