using System.Data;
using Dapper;
using MySqlConnector;
using PermissionsApi.Models;
using Polly;
using Polly.Retry;

namespace PermissionsApi.Services;

public class MySqlPermissionsRepository(string connectionString, IHistoryService historyService, ILogger<MySqlPermissionsRepository> logger)
    : IPermissionsRepository
{
    private readonly ILogger<MySqlPermissionsRepository> _logger = logger;
    
    private static readonly ResiliencePipeline _retryPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            // Retry on all transient errors: deadlocks, connection failures, timeouts, Aurora failovers, etc.
            ShouldHandle = new PredicateBuilder().Handle<MySqlException>(ex => ex.IsTransient)
        })
        .Build();


    private async Task<MySqlConnection> GetConnectionAsync()
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
    }

    #region Permissions

    public async Task<Permission> CreatePermissionAsync(string name, string description, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null)
    {
        return await _retryPipeline.ExecuteAsync(async token =>
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync(token);
            
            const string sql = """
                INSERT INTO permissions (name, description, is_default) 
                VALUES (@Name, @Description, @IsDefault)
                """;
            
            await connection.ExecuteAsync(sql, new { Name = name, Description = description, IsDefault = isDefault }, transaction);
            await transaction.CommitAsync(token);
            
            var permission = new Permission { Name = name, Description = description, IsDefault = isDefault };
            await historyService.RecordChangeAsync("CREATE", "Permission", name, permission, principal, reason);
            
            _logger.LogInformation("Created permission {PermissionName} (IsDefault: {IsDefault})", name, isDefault);
            return permission;
        }, ct);
    }

    public async Task<Permission?> GetPermissionAsync(string name, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = "SELECT name, description, is_default as IsDefault FROM permissions WHERE name = @Name";
        return await connection.QuerySingleOrDefaultAsync<Permission>(sql, new { Name = name });
    }

    public async Task<List<Permission>> GetAllPermissionsAsync(CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = "SELECT name, description, is_default as IsDefault FROM permissions ORDER BY name";
        var result = await connection.QueryAsync<Permission>(sql);
        return result.ToList();
    }

    public async Task<bool> UpdatePermissionAsync(string name, string description, CancellationToken ct, string? principal = null, string? reason = null)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
        const string sql = "UPDATE permissions SET description = @Description WHERE name = @Name";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Name = name, Description = description }, transaction);
        
        if (rowsAffected > 0)
        {
            // Get updated permission in same transaction
            const string selectSql = "SELECT name, description, is_default as IsDefault FROM permissions WHERE name = @Name";
            var permission = await connection.QuerySingleAsync<Permission>(selectSql, new { Name = name }, transaction);
            
            await transaction.CommitAsync(ct);
            await historyService.RecordChangeAsync("UPDATE", "Permission", name, permission, principal, reason);
        }
        
        return rowsAffected > 0;
    }

    public async Task<bool> DeletePermissionAsync(string name, CancellationToken ct, string? principal = null, string? reason = null)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            // Get permission before deletion for history
    const string selectSql = "SELECT name, description, is_default as IsDefault FROM permissions WHERE name = @Name";
    var permission = await connection.QuerySingleOrDefaultAsync<Permission>(selectSql, new { Name = name }, transaction);

    const string deleteSql = "DELETE FROM permissions WHERE name = @Name";
    var rowsAffected = await connection.ExecuteAsync(deleteSql, new { Name = name }, transaction);

    await transaction.CommitAsync(ct);

    if (rowsAffected > 0)
    {
        IEntity entity = permission ?? (IEntity)new EmptyEntity();
        await historyService.RecordChangeAsync("DELETE", "Permission", name, entity, principal, reason);
    }

    return rowsAffected > 0;
    }

    public async Task<bool> SetPermissionDefaultAsync(string name, bool isDefault, CancellationToken ct, string? principal = null, string? reason = null)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
        const string sql = "UPDATE permissions SET is_default = @IsDefault WHERE name = @Name";
        var rowsAffected = await connection.ExecuteAsync(sql, new { Name = name, IsDefault = isDefault }, transaction);

        if (rowsAffected == 0)
            return false;

        // Get updated permission in same transaction
        const string selectSql = "SELECT name, description, is_default as IsDefault FROM permissions WHERE name = @Name";
        var permission = await connection.QuerySingleAsync<Permission>(selectSql, new { Name = name }, transaction);

        await transaction.CommitAsync(ct);
        await historyService.RecordChangeAsync("UPDATE", "Permission", name, permission, principal, reason);

        return true;
    }

    #endregion

    #region Groups

    public async Task<Group> CreateGroupAsync(string name, CancellationToken ct, string? principal = null, string? reason = null)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
        const string sql = "INSERT INTO `groups` (name) VALUES (@Name)";
        await connection.ExecuteAsync(sql, new { Name = name }, transaction);
        
        await transaction.CommitAsync(ct);
        
        var group = new Group { Name = name };
        await historyService.RecordChangeAsync("CREATE", "Group", name, group, principal, reason);
        
        _logger.LogInformation("Created group {GroupName}", name);
        return group;
    }

    public async Task SetGroupPermissionAsync(string groupName, string permission, string access, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
        const string sql = """
            INSERT INTO group_permissions (group_name, permission_name, access_type) 
            VALUES (@GroupName, @Permission, @Access)
            ON DUPLICATE KEY UPDATE access_type = @Access
            """;
        
        await connection.ExecuteAsync(sql, new { GroupName = groupName, Permission = permission, Access = access }, transaction);
        await transaction.CommitAsync(ct);
        
        _logger.LogInformation("Set group {GroupName} permission {Permission} to {Access}", groupName, permission, access);
    }

    public async Task SetGroupPermissionsAsync(string groupName, Dictionary<string, string> permissions, CancellationToken ct, string? principal = null, string? reason = null)
    {
        await _retryPipeline.ExecuteAsync(async token =>
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync(token);
            
            // Clear existing permissions
            const string deleteSql = "DELETE FROM group_permissions WHERE group_name = @GroupName";
            await connection.ExecuteAsync(deleteSql, new { GroupName = groupName }, transaction);

            // Insert new permissions
            if (permissions.Count > 0)
            {
                const string insertSql = """
                    INSERT INTO group_permissions (group_name, permission_name, access_type, assigned_by) 
                    VALUES (@GroupName, @Permission, @Access, @Principal)
                    """;

                var parameters = permissions.Select(p => new 
                { 
                    GroupName = groupName, 
                    Permission = p.Key, 
                    Access = p.Value,
                    Principal = principal
                });

                await connection.ExecuteAsync(insertSql, parameters, transaction);
            }

            // Get updated group in same transaction
            const string selectSql = """
                SELECT g.name, gp.permission_name, gp.access_type
                FROM `groups` g
                LEFT JOIN group_permissions gp ON g.name = gp.group_name
                WHERE g.name = @GroupName
                """;

            var results = await connection.QueryAsync<dynamic>(selectSql, new { GroupName = groupName }, transaction);

            await transaction.CommitAsync(token);

            if (results.Any())
            {
                var groupPermissions = new Dictionary<string, string>();
                foreach (var row in results)
                {
                    if (row.permission_name != null)
                    {
                        groupPermissions[(string)row.permission_name] = (string)row.access_type;
                    }
                }

                var group = new Group { Name = groupName, Permissions = groupPermissions };
                await historyService.RecordChangeAsync("UPDATE", "Group", groupName, group, principal, reason);
            }

            _logger.LogInformation("Set group {GroupName} permissions with {Count} permissions", groupName, permissions.Count);
        }, ct);
    }

    public async Task RemoveGroupPermissionAsync(string groupName, string permission, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            const string sql = "DELETE FROM group_permissions WHERE group_name = @GroupName AND permission_name = @Permission";
    await connection.ExecuteAsync(sql, new { GroupName = groupName, Permission = permission }, transaction);
    await transaction.CommitAsync(ct);

    _logger.LogInformation("Removed group {GroupName} permission {Permission}", groupName, permission);
    }

    public async Task DeleteGroupAsync(string groupName, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            // Get group before deletion for history
    const string selectSql = """
        SELECT g.name, gp.permission_name, gp.access_type
        FROM `groups` g
        LEFT JOIN group_permissions gp ON g.name = gp.group_name
        WHERE g.name = @GroupName
        """;

    var results = await connection.QueryAsync<dynamic>(selectSql, new { GroupName = groupName }, transaction);

    const string deleteSql = "DELETE FROM `groups` WHERE name = @Name";
    var rowsAffected = await connection.ExecuteAsync(deleteSql, new { Name = groupName }, transaction);

    await transaction.CommitAsync(ct);

    if (rowsAffected > 0)
    {
        object groupEntity = new EmptyEntity();

        if (results.Any())
        {
            var permissions = new Dictionary<string, string>();
            foreach (var row in results)
            {
                if (row.permission_name != null)
                {
                    permissions[(string)row.permission_name] = (string)row.access_type;
                }
            }
            groupEntity = new Group { Name = groupName, Permissions = permissions };
        }

        await historyService.RecordChangeAsync("DELETE", "Group", groupName, (IEntity)groupEntity);
        _logger.LogInformation("Deleted group {GroupName}", groupName);
    }
    }

    public async Task<Group?> GetGroupAsync(string name, CancellationToken ct)
    {
        return await GetGroupWithPermissionsAsync(name);
    }

    public async Task<List<Group>> GetAllGroupsAsync(CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT g.name, gp.permission_name, gp.access_type
            FROM `groups` g
            LEFT JOIN group_permissions gp ON g.name = gp.group_name
            ORDER BY g.name, gp.permission_name
            """;
        
        var results = await connection.QueryAsync<dynamic>(sql);
        
        var groupDict = new Dictionary<string, Group>();
        
        foreach (var row in results)
        {
            string groupName = (string)row.name;
            if (!groupDict.TryGetValue(groupName, out var group))
            {
                group = new Group { Name = groupName, Permissions = new Dictionary<string, string>() };
                groupDict[groupName] = group;
            }
            
            if (row.permission_name != null)
            {
                group.Permissions[(string)row.permission_name] = (string)row.access_type;
            }
        }
        
        return groupDict.Values.ToList();
    }

    private async Task<Group?> GetGroupWithPermissionsAsync(string groupName)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT g.name, gp.permission_name, gp.access_type
            FROM `groups` g
            LEFT JOIN group_permissions gp ON g.name = gp.group_name
            WHERE g.name = @GroupName
            """;
        
        var results = await connection.QueryAsync<dynamic>(sql, new { GroupName = groupName });
        
        if (!results.Any()) return null;
        
        var permissions = new Dictionary<string, string>();
        foreach (var row in results)
        {
            if (row.permission_name != null)
            {
                permissions[(string)row.permission_name] = (string)row.access_type;
            }
        }
        
        return new Group { Name = groupName, Permissions = permissions };
    }

    #endregion

    #region Users

    public async Task<User> CreateUserAsync(string email, List<string> groupList, CancellationToken ct, string? principal = null, string? reason = null)
    {
        return await _retryPipeline.ExecuteAsync(async token =>
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync(token);
            
            // Create user
            const string userSql = "INSERT INTO users (email) VALUES (@Email)";
            await connection.ExecuteAsync(userSql, new { Email = email }, transaction);

            // Add group memberships
            if (groupList.Count > 0)
            {
                const string membershipSql = """
                    INSERT INTO user_group_memberships (user_email, group_name, assigned_by) 
                    VALUES (@Email, @GroupName, @Principal)
                    """;

                var parameters = groupList.Select(g => new { Email = email, GroupName = g, Principal = principal });
                await connection.ExecuteAsync(membershipSql, parameters, transaction);
            }

            await transaction.CommitAsync(token);

            var user = new User { Email = email, Groups = groupList };
            await historyService.RecordChangeAsync("CREATE", "User", email, user, principal, reason);

            _logger.LogInformation("Created user {Email} with {GroupCount} groups", email, groupList.Count);
            return user;
        }, ct);
    }

    public async Task SetUserPermissionAsync(string email, string permission, string access, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            const string sql = """
        INSERT INTO user_permissions (user_email, permission_name, access_type) 
        VALUES (@Email, @Permission, @Access)
        ON DUPLICATE KEY UPDATE access_type = @Access
        """;

    await connection.ExecuteAsync(sql, new { Email = email, Permission = permission, Access = access }, transaction);
    await transaction.CommitAsync(ct);

    _logger.LogInformation("Set user {Email} permission {Permission} to {Access}", email, permission, access);
    }

    public async Task SetUserPermissionsAsync(string email, Dictionary<string, string> permissions, CancellationToken ct, string? principal = null, string? reason = null)
    {
        await _retryPipeline.ExecuteAsync(async token =>
        {
            using var connection = await GetConnectionAsync();
            using var transaction = await connection.BeginTransactionAsync(token);
            
            // Clear existing permissions
            const string deleteSql = "DELETE FROM user_permissions WHERE user_email = @Email";
            await connection.ExecuteAsync(deleteSql, new { Email = email }, transaction);

            // Insert new permissions
            if (permissions.Count > 0)
            {
                const string insertSql = """
                    INSERT INTO user_permissions (user_email, permission_name, access_type, assigned_by) 
                    VALUES (@Email, @Permission, @Access, @Principal)
                    """;

                var parameters = permissions.Select(p => new 
                { 
                    Email = email, 
                    Permission = p.Key, 
                    Access = p.Value,
                    Principal = principal
                });

                await connection.ExecuteAsync(insertSql, parameters, transaction);
            }

            // Get updated user in same transaction
            const string selectSql = """
                SELECT u.email,
                       ugm.group_name,
                       up.permission_name,
                       up.access_type
                FROM users u
                LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
                LEFT JOIN user_permissions up ON u.email = up.user_email
                WHERE u.email = @Email
                """;

            var results = await connection.QueryAsync<dynamic>(selectSql, new { Email = email }, transaction);

            await transaction.CommitAsync(token);

            if (results.Any())
            {
                var groups = new List<string>();
                var userPermissions = new Dictionary<string, string>();

                foreach (var row in results)
                {
                    if (row.group_name != null && !groups.Contains((string)row.group_name))
                    {
                        groups.Add((string)row.group_name);
                    }
                    if (row.permission_name != null)
                    {
                        userPermissions[(string)row.permission_name] = (string)row.access_type;
                    }
                }

                var user = new User { Email = email, Groups = groups, Permissions = userPermissions };
                await historyService.RecordChangeAsync("UPDATE", "User", email, user, principal, reason);
            }

            _logger.LogInformation("Set user {Email} permissions with {Count} permissions", email, permissions.Count);
        }, ct);
    }

    public async Task RemoveUserPermissionAsync(string email, string permission, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            const string sql = "DELETE FROM user_permissions WHERE user_email = @Email AND permission_name = @Permission";
    await connection.ExecuteAsync(sql, new { Email = email, Permission = permission }, transaction);
    await transaction.CommitAsync(ct);

    _logger.LogInformation("Removed user {Email} permission {Permission}", email, permission);
    }

    public async Task DeleteUserAsync(string email, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        using var transaction = await connection.BeginTransactionAsync(ct);
        
            // Get user before deletion for history
    const string selectSql = """
        SELECT u.email,
               ugm.group_name,
               up.permission_name,
               up.access_type
        FROM users u
        LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
        LEFT JOIN user_permissions up ON u.email = up.user_email
        WHERE u.email = @Email
        """;

    var results = await connection.QueryAsync<dynamic>(selectSql, new { Email = email }, transaction);

    const string deleteSql = "DELETE FROM users WHERE email = @Email";
    var rowsAffected = await connection.ExecuteAsync(deleteSql, new { Email = email }, transaction);

    await transaction.CommitAsync(ct);

    if (rowsAffected > 0)
    {
        object userEntity = new EmptyEntity();

        if (results.Any())
        {
            var groups = new List<string>();
            var userPermissions = new Dictionary<string, string>();

            foreach (var row in results)
            {
                if (row.group_name != null && !groups.Contains((string)row.group_name))
                {
                    groups.Add((string)row.group_name);
                }
                if (row.permission_name != null)
                {
                    userPermissions[(string)row.permission_name] = (string)row.access_type;
                }
            }

            userEntity = new User { Email = email, Groups = groups, Permissions = userPermissions };
        }

        await historyService.RecordChangeAsync("DELETE", "User", email, (IEntity)userEntity);
        _logger.LogInformation("Deleted user {Email}", email);
    }
    }

    public async Task<User?> GetUserAsync(string email, CancellationToken ct)
    {
        return await GetUserWithPermissionsAsync(email);
    }

    public async Task<List<User>> GetAllUsersAsync(CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT u.email,
                   ugm.group_name,
                   up.permission_name,
                   up.access_type
            FROM users u
            LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
            LEFT JOIN user_permissions up ON u.email = up.user_email
            ORDER BY u.email, ugm.group_name, up.permission_name
            """;
        
        var results = await connection.QueryAsync<dynamic>(sql);
        
        var userDict = new Dictionary<string, User>();
        
        foreach (var row in results)
        {
            string userEmail = (string)row.email;
            if (!userDict.TryGetValue(userEmail, out var user))
            {
                user = new User 
                { 
                    Email = userEmail, 
                    Groups = new List<string>(), 
                    Permissions = new Dictionary<string, string>() 
                };
                userDict[userEmail] = user;
            }
            
            if (row.group_name != null && !user.Groups.Contains((string)row.group_name))
            {
                user.Groups.Add((string)row.group_name);
            }
            
            if (row.permission_name != null)
            {
                user.Permissions[(string)row.permission_name] = (string)row.access_type;
            }
        }
        
        return userDict.Values.ToList();
    }

    private async Task<User?> GetUserWithPermissionsAsync(string email)
    {
        using var connection = await GetConnectionAsync();
        
        const string sql = """
            SELECT u.email,
                   ugm.group_name,
                   up.permission_name,
                   up.access_type
            FROM users u
            LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
            LEFT JOIN user_permissions up ON u.email = up.user_email
            WHERE u.email = @Email
            """;
        
        var results = await connection.QueryAsync<dynamic>(sql, new { Email = email });
        
        if (!results.Any()) return null;
        
        var groups = new List<string>();
        var permissions = new Dictionary<string, string>();
        
        foreach (var row in results)
        {
            if (row.group_name != null && !groups.Contains((string)row.group_name))
            {
                groups.Add((string)row.group_name);
            }
            if (row.permission_name != null)
            {
                permissions[(string)row.permission_name] = (string)row.access_type;
            }
        }
        
        return new User { Email = email, Groups = groups, Permissions = permissions };
    }

    #endregion

    #region Permission Calculation

    public async Task<Dictionary<string, bool>?> CalculatePermissionsAsync(string email, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        // Get user with all related data
        const string sql = """
            SELECT 
                u.email,
                p.name as permission_name,
                p.is_default,
                gp.access_type as group_access,
                g.name as group_name,
                up.access_type as user_access
            FROM users u
            CROSS JOIN permissions p
            LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
            LEFT JOIN `groups` g ON ugm.group_name = g.name
            LEFT JOIN group_permissions gp ON g.name = gp.group_name AND p.name = gp.permission_name
            LEFT JOIN user_permissions up ON u.email = up.user_email AND p.name = up.permission_name
            WHERE u.email = @Email
            ORDER BY p.name, g.name
            """;
        
        var results = await connection.QueryAsync(sql, new { Email = email });
        
        if (!results.Any()) return null;
        
        var permissionResults = new Dictionary<string, bool>();
        
        foreach (var permissionGroup in results.GroupBy(r => r.permission_name))
        {
            var permissionName = permissionGroup.Key;
            var rows = permissionGroup.ToList();
            
            // Start with default
            bool result = rows[0].is_default;
            
            // Apply group permissions (alphabetically by group name)
            foreach (var row in rows.Where(r => r.group_access != null).OrderBy(r => r.group_name))
            {
                result = row.group_access == "ALLOW";
            }
            
            // Apply user override
            var userOverride = rows.Find(r => r.user_access != null);
            if (userOverride != null)
            {
                result = userOverride.user_access == "ALLOW";
            }
            
            permissionResults[permissionName] = result;
        }
        
        _logger.LogDebug("Calculated {PermissionCount} permissions for user {Email}", permissionResults.Count, email);
        return permissionResults;
    }

    public async Task<PermissionDebugResponse?> CalculatePermissionsDebugAsync(string email, CancellationToken ct)
    {
        using var connection = await GetConnectionAsync();
        
        // Get user with all related data for debug chain
        const string sql = """
            SELECT 
                u.email,
                p.name as permission_name,
                p.is_default,
                gp.access_type as group_access,
                g.name as group_name,
                up.access_type as user_access
            FROM users u
            CROSS JOIN permissions p
            LEFT JOIN user_group_memberships ugm ON u.email = ugm.user_email
            LEFT JOIN `groups` g ON ugm.group_name = g.name
            LEFT JOIN group_permissions gp ON g.name = gp.group_name AND p.name = gp.permission_name
            LEFT JOIN user_permissions up ON u.email = up.user_email AND p.name = up.permission_name
            WHERE u.email = @Email
            ORDER BY p.name, g.name
            """;
        
        var results = await connection.QueryAsync(sql, new { Email = email });
        
        if (!results.Any()) return null;
        
        var debugPermissions = new List<PermissionDebugItem>();
        
        foreach (var permissionGroup in results.GroupBy(r => r.permission_name))
        {
            var permissionName = permissionGroup.Key;
            var rows = permissionGroup.ToList();
            
            var chain = new List<PermissionDebugStep>();
            bool finalResult = false;
            
            // Default level
            bool isDefault = rows[0].is_default;
            if (isDefault)
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "Default",
                    Source = "system", 
                    Action = "ALLOW"
                });
                finalResult = true;
            }
            else
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "Default",
                    Source = "system",
                    Action = "NONE"
                });
            }
            
            // Group level - process in alphabetical order
            foreach (var row in rows.Where(r => r.group_access != null).OrderBy(r => r.group_name))
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "Group",
                    Source = row.group_name,
                    Action = row.group_access
                });
                finalResult = row.group_access == "ALLOW";
            }
            
            // User level
            var userOverride = rows.Find(r => r.user_access != null);
            if (userOverride != null)
            {
                chain.Add(new PermissionDebugStep
                {
                    Level = "User",
                    Source = email,
                    Action = userOverride.user_access
                });
                finalResult = userOverride.user_access == "ALLOW";
            }
            
            debugPermissions.Add(new PermissionDebugItem
            {
                Permission = permissionName,
                FinalResult = finalResult ? "ALLOW" : "DENY",
                Chain = chain
            });
        }
        
        return new PermissionDebugResponse 
        { 
            Email = email, 
            Permissions = debugPermissions.OrderBy(p => p.Permission).ToList()
        };
    }

    #endregion
}

// Helper class for when we need to pass an IEntity but don't have one
internal record EmptyEntity : IEntity
{
    public string Id => string.Empty;
}
