using System.Data;
using Dapper;
using MySqlConnector;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;

namespace PermissionsApi.Services;

public class MySqlIntegrityChecker(string connectionString, ILogger<MySqlIntegrityChecker> logger) : IIntegrityChecker
{
    private async Task<IDbConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public async Task<IntegrityCheckResult> CanDeletePermissionAsync(string permissionName)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            
            // Check groups using this permission
            const string groupSql = """
                SELECT DISTINCT g.name 
                FROM groups g 
                INNER JOIN group_permissions gp ON g.name = gp.group_name 
                WHERE gp.permission_name = @PermissionName
                ORDER BY g.name
                """;
            
            var groupsUsingPermission = await connection.QueryAsync<string>(groupSql, new { PermissionName = permissionName });
            var groupsList = groupsUsingPermission.ToList();
            
            if (groupsList.Count > 0)
            {
                return new IntegrityCheckResult(
                    false, 
                    $"Permission is used by groups: {string.Join(", ", groupsList)}"
                );
            }

            // Check users using this permission
            const string userSql = """
                SELECT DISTINCT u.email 
                FROM users u 
                INNER JOIN user_permissions up ON u.email = up.user_email 
                WHERE up.permission_name = @PermissionName
                ORDER BY u.email
                """;
            
            var usersUsingPermission = await connection.QueryAsync<string>(userSql, new { PermissionName = permissionName });
            var usersList = usersUsingPermission.ToList();

            if (usersList.Count > 0)
            {
                return new IntegrityCheckResult(
                    false,
                    $"Permission is used by users: {string.Join(", ", usersList)}"
                );
            }

            return new IntegrityCheckResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check permission deletion integrity for {PermissionName}", permissionName);
            throw new OperationException("Operation failed", ex);
        }
    }

    public async Task<IntegrityCheckResult> CanDeleteGroupAsync(string groupName)
    {
        try
        {
            using var connection = await GetConnectionAsync();
            
            const string sql = """
                SELECT DISTINCT u.email 
                FROM users u 
                INNER JOIN user_group_memberships ugm ON u.email = ugm.user_email 
                WHERE ugm.group_name = @GroupName
                ORDER BY u.email
                """;
            
            var usersInGroup = await connection.QueryAsync<string>(sql, new { GroupName = groupName });
            var usersList = usersInGroup.ToList();

            if (usersList.Count > 0)
            {
                return new IntegrityCheckResult(
                    false,
                    $"Group is assigned to users: {string.Join(", ", usersList)}"
                );
            }

            return new IntegrityCheckResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check group deletion integrity for {GroupName}", groupName);
            throw new OperationException("Operation failed", ex);
        }
    }

    public async Task<PermissionDependencies> GetPermissionDependenciesAsync(string permissionName)
    {
        using var connection = await GetConnectionAsync();
        
        // Get groups using this permission
        const string groupSql = """
            SELECT DISTINCT g.name 
            FROM groups g 
            INNER JOIN group_permissions gp ON g.name = gp.group_name 
            WHERE gp.permission_name = @PermissionName
            ORDER BY g.name
            """;
        
        var groups = await connection.QueryAsync<string>(groupSql, new { PermissionName = permissionName });
        
        // Get users using this permission
        const string userSql = """
            SELECT DISTINCT u.email 
            FROM users u 
            INNER JOIN user_permissions up ON u.email = up.user_email 
            WHERE up.permission_name = @PermissionName
            ORDER BY u.email
            """;
        
        var users = await connection.QueryAsync<string>(userSql, new { PermissionName = permissionName });

        return new PermissionDependencies
        {
            Permission = permissionName,
            Groups = groups.ToList(),
            Users = users.ToList()
        };
    }

    public async Task<GroupDependencies> GetGroupDependenciesAsync(string groupName)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT DISTINCT u.email 
            FROM users u 
            INNER JOIN user_group_memberships ugm ON u.email = ugm.user_email 
            WHERE ugm.group_name = @GroupName
            ORDER BY u.email
            """;
        
        var users = await connection.QueryAsync<string>(sql, new { GroupName = groupName });

        return new GroupDependencies
        {
            GroupName = groupName,
            Users = users.ToList()
        };
    }
}
