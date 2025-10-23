using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class PermissionsRepository : IPermissionsRepository
{
    private readonly ConcurrentDictionary<string, Permission> _permissions = new();
    private readonly ConcurrentDictionary<string, Group> _groups = new();
    private readonly ConcurrentDictionary<string, User> _users = new();
    private readonly Dictionary<string, bool> _defaultPermissions = new() { { "read", true } };
    private readonly ILogger<PermissionsRepository> _logger;

    public PermissionsRepository(ILogger<PermissionsRepository> logger)
    {
        _logger = logger;
    }

    public Task<Permission> CreatePermissionAsync(string name, string description, CancellationToken ct)
    {
        var permission = new Permission { Name = name, Description = description };
        _permissions[name] = permission;
        _logger.LogInformation("Created permission {PermissionName}", name);
        return Task.FromResult(permission);
    }

    public Task<Permission?> GetPermissionAsync(string name, CancellationToken ct)
    {
        _permissions.TryGetValue(name, out var permission);
        return Task.FromResult(permission);
    }

    public Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct)
    {
        return Task.FromResult(_permissions.Values.ToList());
    }

    public Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct)
    {
        if (_permissions.TryGetValue(name, out var permission))
        {
            _permissions[name] = permission with { Description = description };
            _logger.LogInformation("Updated permission {PermissionName}", name);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<bool> DeletePermissionAsync(string name, CancellationToken ct)
    {
        var result = _permissions.TryRemove(name, out _);
        if (result)
            _logger.LogInformation("Deleted permission {PermissionName}", name);
        return Task.FromResult(result);
    }

    public Task<Group> CreateGroupAsync(string name, CancellationToken ct)
    {
        var group = new Group { Id = Guid.NewGuid().ToString(), Name = name };
        _groups[group.Id] = group;
        _logger.LogInformation("Created group {GroupId} with name {GroupName}", group.Id, name);
        return Task.FromResult(group);
    }

    public Task SetGroupPermissionAsync(string groupId, string permission, string access, CancellationToken ct)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.Permissions[permission] = access;
            _logger.LogInformation("Set group {GroupId} permission {Permission} to {Access}", groupId, permission, access);
        }
        return Task.CompletedTask;
    }

    public Task<User> CreateUserAsync(string email, List<string> groups, CancellationToken ct)
    {
        var user = new User { Email = email, Groups = groups };
        _users[email] = user;
        _logger.LogInformation("Created user {Email} with {GroupCount} groups", email, groups.Count);
        return Task.FromResult(user);
    }

    public Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct)
    {
        if (_users.TryGetValue(email, out var user))
        {
            user.Permissions[permission] = access;
            _logger.LogInformation("Set user {Email} permission {Permission} to {Access}", email, permission, access);
        }
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, bool>> CalculatePermissionsAsync(string email, CancellationToken ct)
    {
        var result = new Dictionary<string, bool>(_defaultPermissions);

        if (_users.TryGetValue(email, out var user))
        {
            var userGroups = user.Groups.ToList();
            
            foreach (var groupId in userGroups)
            {
                if (_groups.TryGetValue(groupId, out var group))
                {
                    foreach (var perm in group.Permissions.ToList())
                    {
                        result[perm.Key] = perm.Value == "ALLOW";
                    }
                }
            }

            foreach (var perm in user.Permissions.ToList())
            {
                result[perm.Key] = perm.Value == "ALLOW";
            }
            
            _logger.LogDebug("Calculated {PermissionCount} permissions for user {Email}", result.Count, email);
        }
        else
        {
            _logger.LogDebug("User {Email} not found, returning default permissions", email);
        }

        return Task.FromResult(result);
    }

    public Task DeleteUserAsync(string email, CancellationToken ct)
    {
        var result = _users.TryRemove(email, out _);
        if (result)
            _logger.LogInformation("Deleted user {Email}", email);
        return Task.CompletedTask;
    }

    public Task DeleteGroupAsync(string groupId, CancellationToken ct)
    {
        var result = _groups.TryRemove(groupId, out _);
        if (result)
            _logger.LogInformation("Deleted group {GroupId}", groupId);
        return Task.CompletedTask;
    }
}
