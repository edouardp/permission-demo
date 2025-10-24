namespace PermissionsApi.Models;

public record PermissionDebugResponse
{
    public required string Email { get; init; }
    public List<PermissionDebugItem> Permissions { get; init; } = [];
}

public record PermissionDebugItem
{
    public required string Permission { get; init; }
    public required string FinalResult { get; init; } // "ALLOW" or "DENY"
    public List<PermissionDebugStep> Chain { get; init; } = [];
}

public record PermissionDebugStep
{
    public required string Level { get; init; } // "Default", "Group", "User"
    public required string Source { get; init; } // group name or "default" or user email
    public required string Action { get; init; } // "ALLOW", "DENY", or "NONE"
}
