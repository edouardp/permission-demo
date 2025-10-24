using System.Text.Json.Serialization;

namespace PermissionsApi.Models;

public record Permission : IEntity
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsDefault { get; init; }
    
    public string Id => Name; // Permission ID is the Name
}

public record Group : IEntity
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string> Permissions { get; init; } = new();
}

public record User : IEntity
{
    public required string Email { get; init; }
    public List<string> Groups { get; init; } = [];
    public Dictionary<string, string> Permissions { get; init; } = new();
    
    public string Id => Email; // User ID is the Email
}

public record PermissionAccessRequest : AuditableRequest
{
    public required string Access { get; init; }
}

public record BatchPermissionRequest : AuditableRequest
{
    public List<string> Allow { get; init; } = [];
    public List<string> Deny { get; init; } = [];
}

public record CreateGroupRequest : AuditableRequest
{
    public required string Name { get; init; }
}

public record CreateUserRequest : AuditableRequest
{
    public required string Email { get; init; }
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

public record UpdatePermissionRequest : AuditableRequest
{
    public required string Description { get; init; }
}
