namespace PermissionsApi.Models;

/// <summary>
/// Debug information showing how permissions are resolved for a user
/// </summary>
public record PermissionDebugResponse
{
    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>user@example.com</example>
    public required string Email { get; init; }
    
    /// <summary>
    /// List of all permissions with their resolution chains
    /// </summary>
    public List<PermissionDebugItem> Permissions { get; init; } = [];
}

/// <summary>
/// Debug information for a single permission showing resolution chain
/// </summary>
public record PermissionDebugItem
{
    /// <summary>
    /// Permission name
    /// </summary>
    /// <example>delete</example>
    public required string Permission { get; init; }
    
    /// <summary>
    /// Final resolved access level (ALLOW or DENY)
    /// </summary>
    /// <example>ALLOW</example>
    public required string FinalResult { get; init; }
    
    /// <summary>
    /// Step-by-step resolution chain showing Default → Group → User hierarchy
    /// </summary>
    public List<PermissionDebugStep> Chain { get; init; } = [];
}

/// <summary>
/// A single step in the permission resolution chain
/// </summary>
public record PermissionDebugStep
{
    /// <summary>
    /// Resolution level (Default, Group, or User)
    /// </summary>
    /// <example>Group</example>
    public required string Level { get; init; }
    
    /// <summary>
    /// Source of this permission rule (group name, "system", or user email)
    /// </summary>
    /// <example>editors</example>
    public required string Source { get; init; }
    
    /// <summary>
    /// Action taken at this level (ALLOW, DENY, or NONE)
    /// </summary>
    /// <example>DENY</example>
    public required string Action { get; init; }
}
