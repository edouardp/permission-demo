using Microsoft.Extensions.Logging;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public interface IIntegrityChecker
{
    Task<IntegrityCheckResult> CanDeletePermissionAsync(string permissionName);
    Task<IntegrityCheckResult> CanDeleteGroupAsync(string groupName);
    Task<PermissionDependencies> GetPermissionDependenciesAsync(string permissionName);
    Task<GroupDependencies> GetGroupDependenciesAsync(string groupName);
}

public record IntegrityCheckResult(bool IsValid, string? Reason = null);

public class IntegrityChecker(PermissionsRepository repository, ILogger<IntegrityChecker> logger) : IIntegrityChecker
{
    public Task<IntegrityCheckResult> CanDeletePermissionAsync(string permissionName)
    {
        try
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check permission deletion integrity for {PermissionName}", permissionName);
            throw new OperationException("Operation failed", ex);
        }
    }

    public Task<IntegrityCheckResult> CanDeleteGroupAsync(string groupName)
    {
        try
        {
            var usersInGroup = repository.Users
                .Where(u => u.Value.Groups.Contains(groupName))
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check group deletion integrity for {GroupName}", groupName);
            throw new OperationException("Operation failed", ex);
        }
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

    public Task<GroupDependencies> GetGroupDependenciesAsync(string groupName)
    {
        var users = repository.Users
            .Where(u => u.Value.Groups.Contains(groupName))
            .Select(u => u.Value.Email)
            .OrderBy(e => e)
            .ToList();

        return Task.FromResult(new GroupDependencies
        {
            GroupName = groupName,
            Users = users
        });
    }
}
