using System.Text.Json.Serialization;

namespace PermissionsApi.Models;

/// <summary>
/// A permission that can be granted or denied to users
/// </summary>
public record Permission : IEntity
{
    /// <summary>
    /// Unique permission identifier
    /// </summary>
    /// <example>read</example>
    public required string Name { get; init; }
    
    /// <summary>
    /// Human-readable description of what this permission allows
    /// </summary>
    /// <example>Allows reading of resources</example>
    public required string Description { get; init; }
    
    /// <summary>
    /// Whether this permission is granted to all users by default
    /// </summary>
    /// <example>true</example>
    public bool IsDefault { get; init; }
    
    /// <summary>
    /// Entity ID (same as Name for permissions)
    /// </summary>
    public string Id => Name;
}

/// <summary>
/// A group that users can belong to, with associated permissions
/// </summary>
public record Group : IEntity
{
    /// <summary>
    /// Group name (also serves as unique identifier)
    /// </summary>
    /// <example>editors</example>
    public required string Name { get; init; }
    
    /// <summary>
    /// Permissions assigned to this group (permission name → ALLOW/DENY)
    /// </summary>
    /// <example>{"write": "ALLOW", "delete": "DENY"}</example>
    public Dictionary<string, string> Permissions { get; init; } = new();
    
    /// <summary>
    /// Entity ID (same as Name for groups)
    /// </summary>
    public string Id => Name;
}

/// <summary>
/// A user with group memberships and individual permission overrides
/// </summary>
public record User : IEntity
{
    /// <summary>
    /// User's email address (also serves as unique identifier)
    /// </summary>
    /// <example>user@example.com</example>
    public required string Email { get; init; }
    
    /// <summary>
    /// List of group names this user belongs to
    /// </summary>
    /// <example>["editors", "reviewers"]</example>
    public List<string> Groups { get; init; } = [];
    
    /// <summary>
    /// Individual permission overrides for this user (permission name → ALLOW/DENY)
    /// </summary>
    /// <example>{"delete": "ALLOW"}</example>
    public Dictionary<string, string> Permissions { get; init; } = new();
    
    /// <summary>
    /// Entity ID (same as Email for users)
    /// </summary>
    public string Id => Email;
}

/// <summary>
/// Request to set a single permission to ALLOW or DENY
/// </summary>
public record PermissionAccessRequest : AuditableRequest
{
    /// <summary>
    /// Access level: ALLOW or DENY
    /// </summary>
    /// <example>ALLOW</example>
    public required string Access { get; init; }
}

/// <summary>
/// Request to set multiple permissions at once
/// </summary>
public record BatchPermissionRequest : AuditableRequest
{
    /// <summary>
    /// List of permissions to allow
    /// </summary>
    /// <example>["read", "write"]</example>
    public List<string> Allow { get; init; } = [];
    
    /// <summary>
    /// List of permissions to deny
    /// </summary>
    /// <example>["delete"]</example>
    public List<string> Deny { get; init; } = [];
}

/// <summary>
/// Request to create a new group
/// </summary>
public record CreateGroupRequest : AuditableRequest
{
    /// <summary>
    /// Group name (alphanumeric and hyphens only)
    /// </summary>
    /// <example>content-editors</example>
    public required string Name { get; init; }
}

/// <summary>
/// Request to create a new user
/// </summary>
public record CreateUserRequest : AuditableRequest
{
    /// <summary>
    /// User's email address
    /// </summary>
    /// <example>user@example.com</example>
    public required string Email { get; init; }
    
    /// <summary>
    /// List of group names to add the user to
    /// </summary>
    /// <example>["editors"]</example>
    public List<string> Groups { get; init; } = [];
}

/// <summary>
/// User's calculated permissions after applying Default → Group → User hierarchy
/// </summary>
public record PermissionsResponse
{
    /// <summary>
    /// User email address
    /// </summary>
    /// <example>user@example.com</example>
    public required string Email { get; init; }
    
    /// <summary>
    /// List of permissions the user is allowed to perform
    /// </summary>
    /// <example>["read", "write"]</example>
    public List<string> Allow { get; init; } = [];
    
    /// <summary>
    /// List of permissions the user is explicitly denied
    /// </summary>
    /// <example>["delete"]</example>
    public List<string> Deny { get; init; } = [];
}

/// <summary>
/// Request to create a new permission
/// </summary>
public record CreatePermissionRequest : AuditableRequest
{
    /// <summary>
    /// Unique permission name (e.g., "read", "write", "admin")
    /// </summary>
    /// <example>read</example>
    public required string Name { get; init; }
    
    /// <summary>
    /// Human-readable description of what this permission allows
    /// </summary>
    /// <example>Allows reading of resources</example>
    public required string Description { get; init; }
    
    /// <summary>
    /// Whether this permission should be granted to all users by default
    /// </summary>
    /// <example>true</example>
    [JsonRequired]
    public bool IsDefault { get; init; }
}

/// <summary>
/// Request to update an existing permission's description
/// </summary>
public record UpdatePermissionRequest : AuditableRequest
{
    /// <summary>
    /// New description for the permission
    /// </summary>
    /// <example>Updated description for compliance</example>
    public required string Description { get; init; }
}
