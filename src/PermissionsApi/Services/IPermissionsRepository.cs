using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IPermissionsRepository
{
    Task<Permission> CreatePermissionAsync(string name, string description, bool isDefault, CancellationToken ct);
    Task<Permission?> GetPermissionAsync(string name, CancellationToken ct);
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct);
    Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct);
    Task<bool> DeletePermissionAsync(string name, CancellationToken ct);
    Task<bool> SetPermissionDefaultAsync(string name, bool isDefault, CancellationToken ct);
    
    Task<Group> CreateGroupAsync(string name, CancellationToken ct);
    Task SetGroupPermissionAsync(string groupId, string permission, string access, CancellationToken ct);
    Task ReplaceGroupPermissionsAsync(string groupId, List<PermissionRequest> permissions, CancellationToken ct);
    Task RemoveGroupPermissionAsync(string groupId, string permission, CancellationToken ct);
    Task DeleteGroupAsync(string groupId, CancellationToken ct);
    
    Task<User> CreateUserAsync(string email, List<string> groups, CancellationToken ct);
    Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct);
    Task ReplaceUserPermissionsAsync(string email, List<PermissionRequest> permissions, CancellationToken ct);
    Task RemoveUserPermissionAsync(string email, string permission, CancellationToken ct);
    Task DeleteUserAsync(string email, CancellationToken ct);
    
    Task<Dictionary<string, bool>?> CalculatePermissionsAsync(string email, CancellationToken ct);
}
