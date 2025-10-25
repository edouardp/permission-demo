namespace PermissionsApi.Models;

/// <summary>
/// Dependencies that would prevent deletion of a permission
/// </summary>
public record PermissionDependencies
{
    /// <summary>
    /// Permission name
    /// </summary>
    /// <example>write</example>
    public required string Permission { get; init; }
    
    /// <summary>
    /// Groups that have this permission assigned
    /// </summary>
    /// <example>["editors", "admins"]</example>
    public required List<string> Groups { get; init; }
    
    /// <summary>
    /// Users that have this permission assigned
    /// </summary>
    /// <example>["user@example.com"]</example>
    public required List<string> Users { get; init; }
}

/// <summary>
/// Dependencies that would prevent deletion of a group
/// </summary>
public record GroupDependencies
{
    /// <summary>
    /// Group ID
    /// </summary>
    /// <example>abc123</example>
    public required string GroupId { get; init; }
    
    /// <summary>
    /// Group name
    /// </summary>
    /// <example>editors</example>
    public required string GroupName { get; init; }
    
    /// <summary>
    /// Users that are members of this group
    /// </summary>
    /// <example>["user1@example.com", "user2@example.com"]</example>
    public required List<string> Users { get; init; }
}
