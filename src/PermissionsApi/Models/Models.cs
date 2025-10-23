namespace PermissionsApi.Models;

public record Permission
{
    public required string Name { get; init; }
    public required string Description { get; init; }
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
    public List<string> Groups { get; init; } = new();
    public Dictionary<string, string> Permissions { get; init; } = new();
}

public record PermissionRequest
{
    public required string Permission { get; init; }
    public required string Access { get; init; }
}

public record CreateGroupRequest
{
    public required string Name { get; init; }
}

public record CreateUserRequest
{
    public required string Email { get; init; }
    public List<string> Groups { get; init; } = new();
}

public record PermissionsResponse
{
    public required string Email { get; init; }
    public Dictionary<string, bool> Permissions { get; init; } = new();
}

public record CreatePermissionRequest
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}

public record UpdatePermissionRequest
{
    public required string Description { get; init; }
}
