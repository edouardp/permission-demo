using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IPermissionsRepository
{
    Task<Permission> CreatePermissionAsync(string name, string description, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null);
    Task<Permission?> GetPermissionAsync(string name, CancellationToken ct);
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct);
    Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct, string? principal = null, string? reason = null);
    Task<bool> DeletePermissionAsync(string name, CancellationToken ct, string? principal = null, string? reason = null);
    Task<bool> SetPermissionDefaultAsync(string name, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null);
    
    Task<Group> CreateGroupAsync(string name, CancellationToken ct, string? principal = null, string? reason = null);
    Task SetGroupPermissionAsync(string groupName, string permission, string access, CancellationToken ct);
    Task SetGroupPermissionsAsync(string groupName, Dictionary<string, string> permissions, CancellationToken ct, string? principal = null, string? reason = null);
    Task RemoveGroupPermissionAsync(string groupName, string permission, CancellationToken ct);
    Task DeleteGroupAsync(string groupName, CancellationToken ct);
    
    Task<User> CreateUserAsync(string email, List<string> groupList, CancellationToken ct, string? principal = null, string? reason = null);
    Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct);
    Task SetUserPermissionsAsync(string email, Dictionary<string, string> permissions, CancellationToken ct, string? principal = null, string? reason = null);
    Task RemoveUserPermissionAsync(string email, string permission, CancellationToken ct);
    Task DeleteUserAsync(string email, CancellationToken ct);
    
    Task<Dictionary<string, bool>?> CalculatePermissionsAsync(string email, CancellationToken ct);
    Task<PermissionDebugResponse?> CalculatePermissionsDebugAsync(string email, CancellationToken ct);
}
