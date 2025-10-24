using System.Collections.Concurrent;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class PermissionsRepository(ILogger<PermissionsRepository> logger, IHistoryService historyService)
    : IPermissionsRepository
{
    private readonly ConcurrentDictionary<string, Permission> permissions = new();
    private readonly ConcurrentDictionary<string, Group> groups = new();
    private readonly ConcurrentDictionary<string, User> users = new();

    private Dictionary<string, bool> GetDefaultPermissions()
    {
        var defaults = new Dictionary<string, bool>();
        
        foreach (var permission in permissions.Values.Where(p => p.IsDefault))
        {
            defaults[permission.Name] = true;
        }
        
        return defaults;
    }

    public async Task<Permission> CreatePermissionAsync(string name, string description, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null)
    {
        var permission = new Permission { Name = name, Description = description, IsDefault = isDefault };
        permissions[name] = permission;
        await historyService.RecordChangeAsync("CREATE", "Permission", name, permission, principal, reason);
        logger.LogInformation("Created permission {PermissionName} (IsDefault: {IsDefault})", name, isDefault);
        return permission;
    }

    public Task<Permission?> GetPermissionAsync(string name, CancellationToken ct)
    {
        permissions.TryGetValue(name, out var permission);
        return Task.FromResult(permission);
    }

    public Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct)
    {
        return Task.FromResult(permissions.Values.ToList());
    }

    public async Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct, string? principal = null, string? reason = null)
    {
        if (permissions.TryGetValue(name, out var permission))
        {
            var updatedPermission = permission with { Description = description };
            permissions[name] = updatedPermission;
            await historyService.RecordChangeAsync("UPDATE", "Permission", name, updatedPermission, principal, reason);
            logger.LogInformation("Updated permission {PermissionName}", name);
            return true;
        }
        return false;
    }

    public async Task<bool> DeletePermissionAsync(string name, CancellationToken ct, string? principal = null, string? reason = null)
    {
        var result = permissions.TryRemove(name, out var permission);
        if (result && permission != null)
        {
            await historyService.RecordChangeAsync("DELETE", "Permission", name, permission, principal, reason);
            logger.LogInformation("Deleted permission {PermissionName}", name);
        }
        return result;
    }

    public async Task<bool> SetPermissionDefaultAsync(string name, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null)
    {
        if (permissions.TryGetValue(name, out var permission))
        {
            var updatedPermission = permission with { IsDefault = isDefault };
            permissions[name] = updatedPermission;
            await historyService.RecordChangeAsync("UPDATE", "Permission", name, updatedPermission, principal, reason);
            logger.LogInformation("Set permission {PermissionName} IsDefault to {IsDefault}", name, isDefault);
            return true;
        }
        return false;
    }

    public async Task<Group> CreateGroupAsync(string name, CancellationToken ct, string? principal = null, string? reason = null)
    {
        var group = new Group { Id = Guid.NewGuid().ToString(), Name = name };
        groups[group.Id] = group;
        await historyService.RecordChangeAsync("CREATE", "Group", group.Id, group, principal, reason);
        logger.LogInformation("Created group {GroupId} with name {GroupName}", group.Id, name);
        return group;
    }

    public Task SetGroupPermissionAsync(string groupId, string permission, string access, CancellationToken ct)
    {
        if (groups.TryGetValue(groupId, out var group))
        {
            group.Permissions[permission] = access;
            logger.LogInformation("Set group {GroupId} permission {Permission} to {Access}", groupId, permission, access);
        }
        return Task.CompletedTask;
    }

    public Task ReplaceGroupPermissionsAsync(string groupId, List<PermissionRequest> permissionRequest, CancellationToken ct)
    {
        if (groups.TryGetValue(groupId, out var group))
        {
            group.Permissions.Clear();
            foreach (var permission in permissionRequest)
            {
                group.Permissions[permission.Permission] = permission.Access;
            }
            logger.LogInformation("Replaced group {GroupId} permissions with {Count} permissions", groupId, permissionRequest.Count);
        }
        return Task.CompletedTask;
    }

    public Task RemoveGroupPermissionAsync(string groupId, string permission, CancellationToken ct)
    {
        if (groups.TryGetValue(groupId, out var group))
        {
            group.Permissions.Remove(permission);
            logger.LogInformation("Removed group {GroupId} permission {Permission}", groupId, permission);
        }
        return Task.CompletedTask;
    }

    public async Task<User> CreateUserAsync(string email, List<string> groupList, CancellationToken ct, string? principal = null, string? reason = null)
    {
        var user = new User { Email = email, Groups = groupList };
        users[email] = user;
        await historyService.RecordChangeAsync("CREATE", "User", email, user, principal, reason);
        logger.LogInformation("Created user {Email} with {GroupCount} groups", email, groupList.Count);
        return user;
    }

    public Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct)
    {
        if (users.TryGetValue(email, out var user))
        {
            user.Permissions[permission] = access;
            logger.LogInformation("Set user {Email} permission {Permission} to {Access}", email, permission, access);
        }
        return Task.CompletedTask;
    }

    public Task ReplaceUserPermissionsAsync(string email, List<PermissionRequest> permissionList, CancellationToken ct)
    {
        if (users.TryGetValue(email, out var user))
        {
            user.Permissions.Clear();
            foreach (var permission in permissionList)
            {
                user.Permissions[permission.Permission] = permission.Access;
            }
            logger.LogInformation("Replaced user {Email} permissions with {Count} permissions", email, permissionList.Count);
        }
        return Task.CompletedTask;
    }

    public Task RemoveUserPermissionAsync(string email, string permission, CancellationToken ct)
    {
        if (users.TryGetValue(email, out var user))
        {
            user.Permissions.Remove(permission);
            logger.LogInformation("Removed user {Email} permission {Permission}", email, permission);
        }
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, bool>?> CalculatePermissionsAsync(string email, CancellationToken ct)
    {
        if (!users.TryGetValue(email, out var user))
        {
            logger.LogDebug("User {Email} not found", email);
            return Task.FromResult<Dictionary<string, bool>?>(null);
        }

        var result = GetDefaultPermissions();
        
        // Sort groups alphabetically by name for consistent ordering (last wins)
        var sortedGroups = user.Groups
            .Select(groupId => groups.TryGetValue(groupId, out var g) ? g : null)
            .Where(g => g != null)
            .OrderBy(g => g!.Name)
            .ToList();
        
        foreach (var group in sortedGroups)
        {
            foreach (var perm in group!.Permissions.ToList())
            {
                result[perm.Key] = perm.Value == PermissionAccess.Allow;
            }
        }

        foreach (var perm in user.Permissions.ToList())
        {
            result[perm.Key] = perm.Value == PermissionAccess.Allow;
        }
        
        logger.LogDebug("Calculated {PermissionCount} permissions for user {Email}", result.Count, email);
        return Task.FromResult<Dictionary<string, bool>?>(result);
    }

    public Task<PermissionDebugResponse?> CalculatePermissionsDebugAsync(string email, CancellationToken ct)
    {
        if (!users.TryGetValue(email, out var user))
        {
            return Task.FromResult<PermissionDebugResponse?>(null);
        }

        var allPermissions = new HashSet<string>();
        var defaultPerms = GetDefaultPermissions();
        allPermissions.UnionWith(defaultPerms.Keys);
        
        // Sort groups alphabetically by name for consistent ordering
        var sortedGroups = user.Groups
            .Select(groupId => groups.TryGetValue(groupId, out var g) ? g : null)
            .Where(g => g != null)
            .OrderBy(g => g!.Name)
            .ToList();
        
        foreach (var group in sortedGroups)
        {
            allPermissions.UnionWith(group!.Permissions.Keys);
        }
        allPermissions.UnionWith(user.Permissions.Keys);

        var debugItems = new List<PermissionDebugItem>();
        
        foreach (var permission in allPermissions.OrderBy(p => p))
        {
            var chain = new List<PermissionDebugStep>();
            var finalResult = false;

            // Default level
            if (defaultPerms.TryGetValue(permission, out var defaultValue))
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "Default",
                    Source = "system",
                    Action = defaultValue ? PermissionAccess.Allow : PermissionAccess.Deny
                });
                finalResult = defaultValue;
            }
            else
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "Default",
                    Source = "system",
                    Action = PermissionAccess.None
                });
            }

            // Group level - process in alphabetical order
            foreach (var group in sortedGroups)
            {
                if (group!.Permissions.TryGetValue(permission, out var groupAccess))
                {
                    chain.Add(new PermissionDebugStep
                    {
                        Level = "Group",
                        Source = group.Name,
                        Action = groupAccess
                    });
                    finalResult = groupAccess == PermissionAccess.Allow;
                }
            }

            // User level
            if (user.Permissions.TryGetValue(permission, out var userAccess))
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "User",
                    Source = email,
                    Action = userAccess
                });
                finalResult = userAccess == PermissionAccess.Allow;
            }

            debugItems.Add(new PermissionDebugItem
            {
                Permission = permission,
                FinalResult = finalResult ? PermissionAccess.Allow : PermissionAccess.Deny,
                Chain = chain
            });
        }

        var response = new PermissionDebugResponse
        {
            Email = email,
            Permissions = debugItems
        };

        return Task.FromResult<PermissionDebugResponse?>(response);
    }

    public Task DeleteUserAsync(string email, CancellationToken ct)
    {
        var result = users.TryRemove(email, out _);
        if (result)
            logger.LogInformation("Deleted user {Email}", email);
        return Task.CompletedTask;
    }

    public Task DeleteGroupAsync(string groupId, CancellationToken ct)
    {
        var result = groups.TryRemove(groupId, out _);
        if (result)
            logger.LogInformation("Deleted group {GroupId}", groupId);
        return Task.CompletedTask;
    }
}
