using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IIntegrityChecker
{
    Task<IntegrityCheckResult> CanDeletePermissionAsync(string permissionName);
    Task<IntegrityCheckResult> CanDeleteGroupAsync(string groupId);
    Task<PermissionDependencies> GetPermissionDependenciesAsync(string permissionName);
    Task<GroupDependencies> GetGroupDependenciesAsync(string groupId);
}

public record IntegrityCheckResult(bool IsValid, string? Reason = null);

public class IntegrityChecker(PermissionsRepository repository) : IIntegrityChecker
{
    public Task<IntegrityCheckResult> CanDeletePermissionAsync(string permissionName)
    {
        var groupsUsingPermission = repository.Groups
            .Where(g => g.Value.Permissions.ContainsKey(permissionName))
            .Select(g => g.Value.Name)
            .ToList();

        if (groupsUsingPermission.Count > 0)
        {
            return Task.FromResult(new IntegrityCheckResult(
                false, 
                $"Permission is used by groups: {string.Join(", ", groupsUsingPermission)}"
            ));
        }

        var usersUsingPermission = repository.Users
            .Where(u => u.Value.Permissions.ContainsKey(permissionName))
            .Select(u => u.Value.Email)
            .ToList();

        if (usersUsingPermission.Count > 0)
        {
            return Task.FromResult(new IntegrityCheckResult(
                false,
                $"Permission is used by users: {string.Join(", ", usersUsingPermission)}"
            ));
        }

        return Task.FromResult(new IntegrityCheckResult(true));
    }

    public Task<IntegrityCheckResult> CanDeleteGroupAsync(string groupId)
    {
        var usersInGroup = repository.Users
            .Where(u => u.Value.Groups.Contains(groupId))
            .Select(u => u.Value.Email)
            .ToList();

        if (usersInGroup.Count > 0)
        {
            return Task.FromResult(new IntegrityCheckResult(
                false,
                $"Group is assigned to users: {string.Join(", ", usersInGroup)}"
            ));
        }

        return Task.FromResult(new IntegrityCheckResult(true));
    }

    public Task<PermissionDependencies> GetPermissionDependenciesAsync(string permissionName)
    {
        var groups = repository.Groups
            .Where(g => g.Value.Permissions.ContainsKey(permissionName))
            .Select(g => g.Value.Name)
            .OrderBy(n => n)
            .ToList();

        var users = repository.Users
            .Where(u => u.Value.Permissions.ContainsKey(permissionName))
            .Select(u => u.Value.Email)
            .OrderBy(e => e)
            .ToList();

        return Task.FromResult(new PermissionDependencies
        {
            Permission = permissionName,
            Groups = groups,
            Users = users
        });
    }

    public Task<GroupDependencies> GetGroupDependenciesAsync(string groupId)
    {
        var group = repository.Groups.GetValueOrDefault(groupId);
        var groupName = group?.Name ?? groupId;

        var users = repository.Users
            .Where(u => u.Value.Groups.Contains(groupId))
            .Select(u => u.Value.Email)
            .OrderBy(e => e)
            .ToList();

        return Task.FromResult(new GroupDependencies
        {
            GroupId = groupId,
            GroupName = groupName,
            Users = users
        });
    }
}
