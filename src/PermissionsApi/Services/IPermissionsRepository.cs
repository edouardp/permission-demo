using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IPermissionsRepository
{
    Task<Permission> CreatePermissionAsync(string name, string description, CancellationToken ct);
    Task<Permission?> GetPermissionAsync(string name, CancellationToken ct);
    Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct);
    Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct);
    Task<bool> DeletePermissionAsync(string name, CancellationToken ct);
    
    Task<Group> CreateGroupAsync(string name, CancellationToken ct);
    Task SetGroupPermissionAsync(string groupId, string permission, string access, CancellationToken ct);
    Task DeleteGroupAsync(string groupId, CancellationToken ct);
    
    Task<User> CreateUserAsync(string email, List<string> groups, CancellationToken ct);
    Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct);
    Task DeleteUserAsync(string email, CancellationToken ct);
    
    Task<Dictionary<string, bool>> CalculatePermissionsAsync(string email, CancellationToken ct);
}
