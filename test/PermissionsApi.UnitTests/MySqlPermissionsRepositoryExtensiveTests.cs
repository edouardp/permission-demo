using Microsoft.Extensions.Logging;
using PermissionsApi.Models;
using PermissionsApi.Services;
using Xunit;

namespace PermissionsApi.UnitTests;

[Collection("MySQL")]
public class MySqlPermissionsRepositoryExtensiveTests(MySqlTestFixture fixture)
{
    private readonly MySqlTestFixture _fixture = fixture;

    private MySqlPermissionsRepository CreateRepository()
    {
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repoLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MySqlPermissionsRepository>();
        return new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, repoLogger);
    }

    [Fact]
    public async Task PermissionOperations_ShouldHandleCompleteLifecycle()
    {
        var repo = CreateRepository();
        var permissionName = $"lifecycle-perm-{Guid.NewGuid()}";

        // Create
        var created = await repo.CreatePermissionAsync(permissionName, "Initial description", false, CancellationToken.None);
        Assert.Equal(permissionName, created.Name);
        Assert.Equal("Initial description", created.Description);
        Assert.False(created.IsDefault);

        // Read
        var retrieved = await repo.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Name, retrieved.Name);
        Assert.Equal(created.Description, retrieved.Description);
        Assert.Equal(created.IsDefault, retrieved.IsDefault);

        // Update description
        var updated = await repo.UpdatePermissionAsync(permissionName, "Updated description", CancellationToken.None);
        Assert.True(updated);

        var afterUpdate = await repo.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.Equal("Updated description", afterUpdate!.Description);

        // Update default status
        var defaultUpdated = await repo.SetPermissionDefaultAsync(permissionName, true, CancellationToken.None);
        Assert.True(defaultUpdated);

        var afterDefaultUpdate = await repo.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.True(afterDefaultUpdate!.IsDefault);

        // Delete
        var deleted = await repo.DeletePermissionAsync(permissionName, CancellationToken.None);
        Assert.True(deleted);

        var afterDelete = await repo.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task GroupOperations_ShouldHandleCompleteLifecycle()
    {
        var repo = CreateRepository();
        var groupName = $"lifecycle-group-{Guid.NewGuid()}";

        // Create some permissions first
        var permissions = new List<string>();
        for (int i = 0; i < 5; i++)
        {
            var permName = $"group-perm-{i}-{Guid.NewGuid()}";
            permissions.Add(permName);
            await repo.CreatePermissionAsync(permName, $"Permission {i}", false, CancellationToken.None);
        }

        // Create group
        var created = await repo.CreateGroupAsync(groupName, CancellationToken.None);
        Assert.Equal(groupName, created.Name);
        Assert.NotNull(created.Permissions);

        // Set individual permission
        await repo.SetGroupPermissionAsync(groupName, permissions[0], "ALLOW", CancellationToken.None);

        // Set batch permissions
        var batchPermissions = new Dictionary<string, string>
        {
            [permissions[1]] = "ALLOW",
            [permissions[2]] = "DENY",
            [permissions[3]] = "ALLOW"
        };
        await repo.SetGroupPermissionsAsync(groupName, batchPermissions, CancellationToken.None);

        // Verify permissions were set
        var retrieved = await repo.GetGroupAsync(groupName, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Permissions.Count);
        Assert.Equal("ALLOW", retrieved.Permissions[permissions[1]]);
        Assert.Equal("DENY", retrieved.Permissions[permissions[2]]);
        Assert.Equal("ALLOW", retrieved.Permissions[permissions[3]]);

        // Remove individual permission
        await repo.RemoveGroupPermissionAsync(groupName, permissions[1], CancellationToken.None);

        var afterRemove = await repo.GetGroupAsync(groupName, CancellationToken.None);
        Assert.Equal(2, afterRemove!.Permissions.Count);
        Assert.False(afterRemove.Permissions.ContainsKey(permissions[1]));

        // Delete group
        await repo.DeleteGroupAsync(groupName, CancellationToken.None);

        var afterDelete = await repo.GetGroupAsync(groupName, CancellationToken.None);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task UserOperations_ShouldHandleCompleteLifecycle()
    {
        var repo = CreateRepository();
        var userEmail = $"lifecycle-{Guid.NewGuid()}@test.com";

        // Create groups and permissions
        var groups = new List<string>();
        var permissions = new List<string>();
        
        for (int i = 0; i < 3; i++)
        {
            var groupName = $"user-group-{i}-{Guid.NewGuid()}";
            groups.Add(groupName);
            await repo.CreateGroupAsync(groupName, CancellationToken.None);
        }

        for (int i = 0; i < 5; i++)
        {
            var permName = $"user-perm-{i}-{Guid.NewGuid()}";
            permissions.Add(permName);
            await repo.CreatePermissionAsync(permName, $"Permission {i}", false, CancellationToken.None);
        }

        // Create user with groups
        var created = await repo.CreateUserAsync(userEmail, groups.Take(2).ToList(), CancellationToken.None);
        Assert.Equal(userEmail, created.Email);
        Assert.Equal(2, created.Groups.Count);

        // Set individual permission
        await repo.SetUserPermissionAsync(userEmail, permissions[0], "ALLOW", CancellationToken.None);

        // Set batch permissions
        var batchPermissions = new Dictionary<string, string>
        {
            [permissions[1]] = "DENY",
            [permissions[2]] = "ALLOW",
            [permissions[3]] = "DENY"
        };
        await repo.SetUserPermissionsAsync(userEmail, batchPermissions, CancellationToken.None);

        // Verify user state
        var retrieved = await repo.GetUserAsync(userEmail, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved.Groups.Count);
        Assert.Equal(3, retrieved.Permissions.Count);
        Assert.Equal("DENY", retrieved.Permissions[permissions[1]]);
        Assert.Equal("ALLOW", retrieved.Permissions[permissions[2]]);
        Assert.Equal("DENY", retrieved.Permissions[permissions[3]]);

        // Remove individual permission
        await repo.RemoveUserPermissionAsync(userEmail, permissions[1], CancellationToken.None);

        var afterRemove = await repo.GetUserAsync(userEmail, CancellationToken.None);
        Assert.Equal(2, afterRemove!.Permissions.Count);
        Assert.False(afterRemove.Permissions.ContainsKey(permissions[1]));

        // Delete user
        await repo.DeleteUserAsync(userEmail, CancellationToken.None);

        var afterDelete = await repo.GetUserAsync(userEmail, CancellationToken.None);
        Assert.Null(afterDelete);
    }

    [Fact]
    public async Task PermissionCalculation_ShouldHandleComplexScenarios()
    {
        var repo = CreateRepository();
        var random = new Random(42);

        // Create permissions with mixed defaults
        var permissions = new List<string>();
        var defaultPermissions = new List<string>();
        
        for (int i = 0; i < 10; i++)
        {
            var permName = $"calc-perm-{i}";
            permissions.Add(permName);
            var isDefault = i < 3; // First 3 are default
            if (isDefault) defaultPermissions.Add(permName);
            
            await repo.CreatePermissionAsync(permName, $"Permission {i}", isDefault, CancellationToken.None);
        }

        // Create groups with conflicting permissions
        var group1 = "calc-group-1";
        var group2 = "calc-group-2";
        var group3 = "calc-group-3";

        await repo.CreateGroupAsync(group1, CancellationToken.None);
        await repo.CreateGroupAsync(group2, CancellationToken.None);
        await repo.CreateGroupAsync(group3, CancellationToken.None);

        // Group 1: Allow some, deny others
        await repo.SetGroupPermissionsAsync(group1, new Dictionary<string, string>
        {
            [permissions[0]] = "ALLOW",  // Override default
            [permissions[3]] = "ALLOW",  // New permission
            [permissions[4]] = "DENY"    // Deny permission
        }, CancellationToken.None);

        // Group 2: Different permissions (alphabetically after group1)
        await repo.SetGroupPermissionsAsync(group2, new Dictionary<string, string>
        {
            [permissions[0]] = "DENY",   // Override group1
            [permissions[3]] = "DENY",   // Override group1
            [permissions[5]] = "ALLOW"   // New permission
        }, CancellationToken.None);

        // Group 3: More overrides (alphabetically after group2)
        await repo.SetGroupPermissionsAsync(group3, new Dictionary<string, string>
        {
            [permissions[4]] = "ALLOW",  // Override group1 DENY
            [permissions[6]] = "ALLOW"   // New permission
        }, CancellationToken.None);

        // Create user with all groups (order matters: alphabetical processing)
        var userEmail = "complex-calc@test.com";
        await repo.CreateUserAsync(userEmail, [group1, group2, group3], CancellationToken.None);

        // Add user-specific overrides
        await repo.SetUserPermissionsAsync(userEmail, new Dictionary<string, string>
        {
            [permissions[0]] = "ALLOW",  // Override groups
            [permissions[5]] = "DENY",   // Override group2
            [permissions[7]] = "ALLOW"   // New permission
        }, CancellationToken.None);

        // Calculate permissions
        var calculated = await repo.CalculatePermissionsAsync(userEmail, CancellationToken.None);
        var debug = await repo.CalculatePermissionsDebugAsync(userEmail, CancellationToken.None);

        Assert.NotNull(calculated);
        Assert.NotNull(debug);

        // Verify expected results based on resolution order
        // Default -> Group1 -> Group2 -> Group3 -> User
        
        // permissions[0]: Default(ALLOW) -> Group1(ALLOW) -> Group2(DENY) -> User(ALLOW) = ALLOW
        Assert.True(calculated[permissions[0]]);
        
        // permissions[1]: Default(ALLOW) -> no group changes -> User(no change) = ALLOW
        Assert.True(calculated[permissions[1]]);
        
        // permissions[2]: Default(ALLOW) -> no group changes -> User(no change) = ALLOW
        Assert.True(calculated[permissions[2]]);
        
        // permissions[3]: Default(false) -> Group1(ALLOW) -> Group2(DENY) -> User(no change) = DENY
        Assert.False(calculated[permissions[3]]);
        
        // permissions[4]: Default(false) -> Group1(DENY) -> Group3(ALLOW) -> User(no change) = ALLOW
        Assert.True(calculated[permissions[4]]);
        
        // permissions[5]: Default(false) -> Group2(ALLOW) -> User(DENY) = DENY
        Assert.False(calculated[permissions[5]]);
        
        // permissions[6]: Default(false) -> Group3(ALLOW) -> User(no change) = ALLOW
        Assert.True(calculated[permissions[6]]);
        
        // permissions[7]: Default(false) -> no group changes -> User(ALLOW) = ALLOW
        Assert.True(calculated[permissions[7]]);

        // Verify debug information
        var perm0Debug = debug.Permissions.First(p => p.Permission == permissions[0]);
        Assert.Equal("ALLOW", perm0Debug.FinalResult);
        Assert.True(perm0Debug.Chain.Count >= 4); // Default + Group1 + Group2 + User

        // Verify chain contains expected levels
        var chainLevels = perm0Debug.Chain.Select(c => c.Level).ToList();
        Assert.Contains("Default", chainLevels);
        Assert.Contains("Group", chainLevels);
        Assert.Contains("User", chainLevels);
    }

    [Fact]
    public async Task TransactionIntegrity_ShouldMaintainConsistency()
    {
        var repo = CreateRepository();
        var permissionName = $"tx-perm-{Guid.NewGuid()}";
        var groupName = $"tx-group-{Guid.NewGuid()}";
        var userEmail = $"tx-user-{Guid.NewGuid()}@test.com";

        // Create entities
        await repo.CreatePermissionAsync(permissionName, "Transaction test", false, CancellationToken.None);
        await repo.CreateGroupAsync(groupName, CancellationToken.None);
        await repo.CreateUserAsync(userEmail, [groupName], CancellationToken.None);

        // Test concurrent modifications
        var tasks = new List<Task>();
        
        // Concurrent permission assignments
        for (int i = 0; i < 10; i++)
        {
            var access = i % 2 == 0 ? "ALLOW" : "DENY";
            tasks.Add(repo.SetGroupPermissionAsync(groupName, permissionName, access, CancellationToken.None));
            tasks.Add(repo.SetUserPermissionAsync(userEmail, permissionName, access, CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        // Verify final state is consistent
        var group = await repo.GetGroupAsync(groupName, CancellationToken.None);
        var user = await repo.GetUserAsync(userEmail, CancellationToken.None);
        var calculated = await repo.CalculatePermissionsAsync(userEmail, CancellationToken.None);

        Assert.NotNull(group);
        Assert.NotNull(user);
        Assert.NotNull(calculated);

        // Verify permission exists in either ALLOW or DENY state (not corrupted)
        if (group.Permissions.ContainsKey(permissionName))
        {
            Assert.True(group.Permissions[permissionName] == "ALLOW" || group.Permissions[permissionName] == "DENY");
        }
        
        if (user.Permissions.ContainsKey(permissionName))
        {
            Assert.True(user.Permissions[permissionName] == "ALLOW" || user.Permissions[permissionName] == "DENY");
        }
    }

    [Fact]
    public async Task EdgeCases_ShouldHandleGracefully()
    {
        var repo = CreateRepository();

        // Test non-existent entities
        var nonExistentPerm = await repo.GetPermissionAsync("non-existent", CancellationToken.None);
        Assert.Null(nonExistentPerm);

        var nonExistentGroup = await repo.GetGroupAsync("non-existent", CancellationToken.None);
        Assert.Null(nonExistentGroup);

        var nonExistentUser = await repo.GetUserAsync("non-existent@test.com", CancellationToken.None);
        Assert.Null(nonExistentUser);

        // Test calculations for non-existent user
        var nonExistentCalc = await repo.CalculatePermissionsAsync("non-existent@test.com", CancellationToken.None);
        Assert.Null(nonExistentCalc);

        var nonExistentDebug = await repo.CalculatePermissionsDebugAsync("non-existent@test.com", CancellationToken.None);
        Assert.Null(nonExistentDebug);

        // Test updates on non-existent entities
        var updateResult = await repo.UpdatePermissionAsync("non-existent", "New desc", CancellationToken.None);
        Assert.False(updateResult);

        var deleteResult = await repo.DeletePermissionAsync("non-existent", CancellationToken.None);
        Assert.False(deleteResult);

        // Test empty collections
        var emptyPermissions = new Dictionary<string, string>();
        var testGroup = $"empty-test-{Guid.NewGuid()}";
        await repo.CreateGroupAsync(testGroup, CancellationToken.None);
        await repo.SetGroupPermissionsAsync(testGroup, emptyPermissions, CancellationToken.None);

        var groupAfterEmpty = await repo.GetGroupAsync(testGroup, CancellationToken.None);
        Assert.NotNull(groupAfterEmpty);
        Assert.Empty(groupAfterEmpty.Permissions);
    }

    [Fact]
    public async Task DataConsistency_ShouldMaintainReferentialIntegrity()
    {
        var repo = CreateRepository();
        var permissionName = $"ref-perm-{Guid.NewGuid()}";
        var groupName = $"ref-group-{Guid.NewGuid()}";
        var userEmail = $"ref-user-{Guid.NewGuid()}@test.com";

        // Create entities with relationships
        await repo.CreatePermissionAsync(permissionName, "Reference test", false, CancellationToken.None);
        await repo.CreateGroupAsync(groupName, CancellationToken.None);
        await repo.SetGroupPermissionAsync(groupName, permissionName, "ALLOW", CancellationToken.None);
        await repo.CreateUserAsync(userEmail, [groupName], CancellationToken.None);
        await repo.SetUserPermissionAsync(userEmail, permissionName, "DENY", CancellationToken.None);

        // Verify relationships exist
        var group = await repo.GetGroupAsync(groupName, CancellationToken.None);
        var user = await repo.GetUserAsync(userEmail, CancellationToken.None);

        Assert.Contains(permissionName, group!.Permissions.Keys);
        Assert.Contains(groupName, user!.Groups);
        Assert.Contains(permissionName, user.Permissions.Keys);

        // Test cascading deletes work correctly
        await repo.DeleteUserAsync(userEmail, CancellationToken.None);
        
        // Group should still exist and have the permission
        var groupAfterUserDelete = await repo.GetGroupAsync(groupName, CancellationToken.None);
        Assert.NotNull(groupAfterUserDelete);
        Assert.Contains(permissionName, groupAfterUserDelete.Permissions.Keys);

        await repo.DeleteGroupAsync(groupName, CancellationToken.None);
        
        // Permission should still exist
        var permAfterGroupDelete = await repo.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.NotNull(permAfterGroupDelete);
    }
}
