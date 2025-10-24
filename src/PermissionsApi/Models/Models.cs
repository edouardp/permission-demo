namespace PermissionsApi.Models;

public record Permission
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsDefault { get; init; }
}

public record Group
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string> Permissions { get; init; } = new();
}

public record User
{
    public required string Email { get; init; }
    public List<string> Groups { get; init; } = [];
    public Dictionary<string, string> Permissions { get; init; } = new();
}

public record PermissionRequest
{
    public required string Permission { get; init; }
    public required string Access { get; init; }
}

public record PermissionAccessRequest : AuditableRequest
{
    public required string Access { get; init; }
}

public record BatchPermissionRequest : AuditableRequest
{
    public required List<PermissionRequest> Permissions { get; init; } = [];
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

public record PermissionsResponse
{
    public required string Email { get; init; }
    public List<string> Allow { get; init; } = [];
    public List<string> Deny { get; init; } = [];
}

public record CreatePermissionRequest : AuditableRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsDefault { get; init; }
}

public record UpdatePermissionRequest : AuditableRequest
{
    public required string Description { get; init; }
}
