using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class PermissionsRepository
{
    private readonly Dictionary<string, Permission> _permissions = new();
    private readonly Dictionary<string, Group> _groups = new();
    private readonly Dictionary<string, User> _users = new();
    private readonly Dictionary<string, bool> _defaultPermissions = new() { { "read", true } };

    public Permission CreatePermission(string name, string description)
    {
        var permission = new Permission { Name = name, Description = description };
        _permissions[name] = permission;
        return permission;
    }

    public Permission? GetPermission(string name)
    {
        _permissions.TryGetValue(name, out var permission);
        return permission;
    }

    public List<Permission> GetAllPermissions()
    {
        return _permissions.Values.ToList();
    }

    public bool UpdatePermission(string name, string description)
    {
        if (_permissions.TryGetValue(name, out var permission))
        {
            permission.Description = description;
            return true;
        }
        return false;
    }

    public bool DeletePermission(string name)
    {
        return _permissions.Remove(name);
    }

    public Group CreateGroup(string name)
    {
        var group = new Group { Name = name };
        _groups[group.Id] = group;
        return group;
    }

    public void SetGroupPermission(string groupId, string permission, string access)
    {
        if (_groups.TryGetValue(groupId, out var group))
        {
            group.Permissions[permission] = access;
        }
    }

    public User CreateUser(string email, List<string> groups)
    {
        var user = new User { Email = email, Groups = groups };
        _users[email] = user;
        return user;
    }

    public void SetUserPermission(string email, string permission, string access)
    {
        if (_users.TryGetValue(email, out var user))
        {
            user.Permissions[permission] = access;
        }
    }

    public Dictionary<string, bool> CalculatePermissions(string email)
    {
        var result = new Dictionary<string, bool>(_defaultPermissions);

        if (_users.TryGetValue(email, out var user))
        {
            foreach (var groupId in user.Groups)
            {
                if (_groups.TryGetValue(groupId, out var group))
                {
                    foreach (var perm in group.Permissions)
                    {
                        result[perm.Key] = perm.Value == "ALLOW";
                    }
                }
            }

            foreach (var perm in user.Permissions)
            {
                result[perm.Key] = perm.Value == "ALLOW";
            }
        }

        return result;
    }

    public void DeleteUser(string email)
    {
        _users.Remove(email);
    }

    public void DeleteGroup(string groupId)
    {
        _groups.Remove(groupId);
    }
}
