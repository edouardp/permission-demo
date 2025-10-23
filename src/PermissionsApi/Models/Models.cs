namespace PermissionsApi.Models;

public class Group
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Permissions { get; set; } = new();
}

public class User
{
    public string Email { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = new();
    public Dictionary<string, string> Permissions { get; set; } = new();
}

public class PermissionRequest
{
    public string Permission { get; set; } = string.Empty;
    public string Access { get; set; } = string.Empty;
}

public class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateUserRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Groups { get; set; } = new();
}

public class PermissionsResponse
{
    public string Email { get; set; } = string.Empty;
    public Dictionary<string, bool> Permissions { get; set; } = new();
}
