using Microsoft.Extensions.Logging;
using PermissionsApi.Models;
using PermissionsApi.Services;
using Xunit;

namespace PermissionsApi.UnitTests;

[Collection("MySQL")]
public class MySqlPermissionsRepositoryScaleTests(MySqlTestFixture fixture)
{
    private readonly MySqlTestFixture _fixture = fixture;

    private MySqlPermissionsRepository CreateRepository()
    {
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repoLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MySqlPermissionsRepository>();
        return new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, repoLogger);
    }

    [Fact]
    public async Task ScaleTest_ShouldHandle100Permissions()
    {
        var repo = CreateRepository();
        var random = new Random(42);
        var permissions = new List<Permission>();

        // Create 100 permissions with random properties
        var tasks = new List<Task<Permission>>();
        for (int i = 0; i < 100; i++)
        {
            var name = $"scale-perm-{i}-{Guid.NewGuid()}";
            var description = $"Scale test permission {i} - {random.Next(1000, 9999)}";
            var isDefault = random.Next(2) == 0;
            
            tasks.Add(repo.CreatePermissionAsync(name, description, isDefault, CancellationToken.None));
        }

        permissions.AddRange(await Task.WhenAll(tasks));

        // Verify all permissions were created
        var allPermissions = await repo.GetAllPermissionsAsync(CancellationToken.None);
        foreach (var perm in permissions)
        {
            Assert.Contains(allPermissions, p => p.Name == perm.Name);
        }

        // Test batch updates
        var updateTasks = permissions.Select(async perm =>
        {
            var newDescription = $"Updated: {perm.Description}";
            return await repo.UpdatePermissionAsync(perm.Name, newDescription, CancellationToken.None);
        });

        await Task.WhenAll(updateTasks);

        // Test default status changes
        var defaultTasks = permissions.Take(50).Select(async perm =>
        {
            return await repo.SetPermissionDefaultAsync(perm.Name, !perm.IsDefault, CancellationToken.None);
        });

        await Task.WhenAll(defaultTasks);

        // Verify changes
        var updatedPermissions = await repo.GetAllPermissionsAsync(CancellationToken.None);
        Assert.True(updatedPermissions.Count >= 100);
    }

    [Fact]
    public async Task ScaleTest_ShouldHandle100Groups()
    {
        var repo = CreateRepository();
        var random = new Random(42);
        var groups = new List<Group>();

        // Create some permissions first
        var permissions = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            var permName = $"group-perm-{i}";
            permissions.Add(permName);
            await repo.CreatePermissionAsync(permName, $"Permission {i}", false, CancellationToken.None);
        }

        // Create 100 groups
        var tasks = new List<Task<Group>>();
        for (int i = 0; i < 100; i++)
        {
            var name = $"scale-group-{i}-{Guid.NewGuid()}";
            tasks.Add(repo.CreateGroupAsync(name, CancellationToken.None));
        }

        groups.AddRange(await Task.WhenAll(tasks));

        // Assign random permissions to groups
        var permissionTasks = new List<Task>();
        foreach (var group in groups)
        {
            var numPerms = random.Next(1, 10);
            var groupPermissions = new Dictionary<string, string>();
            
            for (int i = 0; i < numPerms; i++)
            {
                var perm = permissions[random.Next(permissions.Count)];
                var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                groupPermissions[perm] = access;
            }
            
            permissionTasks.Add(repo.SetGroupPermissionsAsync(group.Name, groupPermissions, CancellationToken.None));
        }

        await Task.WhenAll(permissionTasks);

        // Verify all groups exist and have permissions
        var allGroups = await repo.GetAllGroupsAsync(CancellationToken.None);
        Assert.True(allGroups.Count >= 100);

        foreach (var group in groups.Take(10)) // Sample check
        {
            var retrievedGroup = await repo.GetGroupAsync(group.Name, CancellationToken.None);
            Assert.NotNull(retrievedGroup);
        }
    }

    [Fact]
    public async Task ScaleTest_ShouldHandle1000Users()
    {
        var repo = CreateRepository();
        var random = new Random(42);

        // Create groups first
        var groups = new List<string>();
        for (int i = 0; i < 20; i++)
        {
            var groupName = $"user-group-{i}";
            groups.Add(groupName);
            await repo.CreateGroupAsync(groupName, CancellationToken.None);
        }

        // Create permissions
        var permissions = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var permName = $"user-perm-{i}";
            permissions.Add(permName);
            await repo.CreatePermissionAsync(permName, $"User permission {i}", false, CancellationToken.None);
        }

        // Create 1000 users in batches
        var users = new List<User>();
        const int batchSize = 100;
        
        for (int batch = 0; batch < 10; batch++)
        {
            var batchTasks = new List<Task<User>>();
            
            for (int i = 0; i < batchSize; i++)
            {
                var userIndex = batch * batchSize + i;
                var email = $"scale-user-{userIndex}@test.com";
                
                // Assign random groups
                var numGroups = random.Next(0, 5);
                var userGroups = new List<string>();
                for (int j = 0; j < numGroups; j++)
                {
                    var group = groups[random.Next(groups.Count)];
                    if (!userGroups.Contains(group))
                    {
                        userGroups.Add(group);
                    }
                }
                
                batchTasks.Add(repo.CreateUserAsync(email, userGroups, CancellationToken.None));
            }
            
            var batchUsers = await Task.WhenAll(batchTasks);
            users.AddRange(batchUsers);
        }

        // Assign random direct permissions to users
        var permissionTasks = new List<Task>();
        foreach (var user in users.Take(500)) // Half the users get direct permissions
        {
            if (random.Next(2) == 0)
            {
                var userPermissions = new Dictionary<string, string>();
                var numPerms = random.Next(1, 5);
                
                for (int i = 0; i < numPerms; i++)
                {
                    var perm = permissions[random.Next(permissions.Count)];
                    var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                    userPermissions[perm] = access;
                }
                
                permissionTasks.Add(repo.SetUserPermissionsAsync(user.Email, userPermissions, CancellationToken.None));
            }
        }

        await Task.WhenAll(permissionTasks);

        // Verify users exist
        var allUsers = await repo.GetAllUsersAsync(CancellationToken.None);
        Assert.True(allUsers.Count >= 1000);

        // Test permission calculations for sample users
        var calculationTasks = users.Take(100).Select(async user =>
        {
            var permissions = await repo.CalculatePermissionsAsync(user.Email, CancellationToken.None);
            Assert.NotNull(permissions);
            return permissions;
        });

        var calculatedPermissions = await Task.WhenAll(calculationTasks);
        Assert.All(calculatedPermissions, perms => Assert.NotNull(perms));
    }

    [Fact]
    public async Task ScaleTest_ShouldHandleComplexPermissionCalculations()
    {
        var repo = CreateRepository();
        var random = new Random(42);

        // Create 50 permissions with mixed defaults
        var permissions = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            var permName = $"calc-perm-{i}";
            permissions.Add(permName);
            var isDefault = random.Next(3) == 0; // 1/3 are default
            await repo.CreatePermissionAsync(permName, $"Calculation permission {i}", isDefault, CancellationToken.None);
        }

        // Create 30 groups with complex permission matrices
        var groups = new List<string>();
        for (int i = 0; i < 30; i++)
        {
            var groupName = $"calc-group-{i}";
            groups.Add(groupName);
            await repo.CreateGroupAsync(groupName, CancellationToken.None);
            
            // Each group gets 10-20 random permissions
            var groupPermissions = new Dictionary<string, string>();
            var numPerms = random.Next(10, 21);
            var selectedPerms = permissions.OrderBy(x => random.Next()).Take(numPerms);
            
            foreach (var perm in selectedPerms)
            {
                var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                groupPermissions[perm] = access;
            }
            
            await repo.SetGroupPermissionsAsync(groupName, groupPermissions, CancellationToken.None);
        }

        // Create 200 users with complex group memberships
        var users = new List<string>();
        for (int i = 0; i < 200; i++)
        {
            var email = $"calc-user-{i}@test.com";
            users.Add(email);
            
            // Each user belongs to 1-5 groups
            var numGroups = random.Next(1, 6);
            var userGroups = groups.OrderBy(x => random.Next()).Take(numGroups).ToList();
            
            await repo.CreateUserAsync(email, userGroups, CancellationToken.None);
            
            // Some users get direct permission overrides
            if (random.Next(3) == 0) // 1/3 get overrides
            {
                var userPermissions = new Dictionary<string, string>();
                var numOverrides = random.Next(1, 8);
                var selectedPerms = permissions.OrderBy(x => random.Next()).Take(numOverrides);
                
                foreach (var perm in selectedPerms)
                {
                    var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                    userPermissions[perm] = access;
                }
                
                await repo.SetUserPermissionsAsync(email, userPermissions, CancellationToken.None);
            }
        }

        // Test permission calculations for all users
        var calculationTasks = users.Select(async email =>
        {
            var calculated = await repo.CalculatePermissionsAsync(email, CancellationToken.None);
            var debug = await repo.CalculatePermissionsDebugAsync(email, CancellationToken.None);
            
            Assert.NotNull(calculated);
            Assert.NotNull(debug);
            Assert.Equal(email, debug.Email);
            
            // Verify debug chain makes sense
            foreach (var permDebug in debug.Permissions)
            {
                Assert.NotEmpty(permDebug.Chain);
                Assert.True(permDebug.Chain.Count >= 1); // At least default level
                
                // Verify chain ordering: Default -> Groups -> User
                var levels = permDebug.Chain.Select(c => c.Level).ToList();
                var expectedOrder = new[] { "Default", "Group", "User" };
                
                for (int i = 1; i < levels.Count; i++)
                {
                    var currentIndex = Array.IndexOf(expectedOrder, levels[i]);
                    var previousIndex = Array.IndexOf(expectedOrder, levels[i-1]);
                    Assert.True(currentIndex >= previousIndex, $"Chain order violation: {string.Join(" -> ", levels)}");
                }
            }
            
            return new { Email = email, Calculated = calculated, Debug = debug };
        });

        var results = await Task.WhenAll(calculationTasks);

        // Verify all calculations completed successfully
        Assert.All(results, r => 
        {
            Assert.NotNull(r.Calculated);
            Assert.NotNull(r.Debug);
        });

        // Test concurrent calculations
        var concurrentTasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var email = users[random.Next(users.Count)];
            concurrentTasks.Add(repo.CalculatePermissionsAsync(email, CancellationToken.None));
            concurrentTasks.Add(repo.CalculatePermissionsDebugAsync(email, CancellationToken.None));
        }

        await Task.WhenAll(concurrentTasks);
    }

    [Fact]
    public async Task StressTest_ShouldHandleConcurrentOperations()
    {
        var repo = CreateRepository();
        var random = new Random(42);
        var tasks = new List<Task>();

        // Concurrent permission operations
        for (int i = 0; i < 50; i++)
        {
            var permName = $"stress-perm-{i}";
            tasks.Add(repo.CreatePermissionAsync(permName, $"Stress permission {i}", random.Next(2) == 0, CancellationToken.None));
        }

        // Concurrent group operations
        for (int i = 0; i < 30; i++)
        {
            var groupName = $"stress-group-{i}";
            tasks.Add(repo.CreateGroupAsync(groupName, CancellationToken.None));
        }

        // Concurrent user operations
        for (int i = 0; i < 100; i++)
        {
            var email = $"stress-user-{i}@test.com";
            tasks.Add(repo.CreateUserAsync(email, [], CancellationToken.None));
        }

        await Task.WhenAll(tasks);

        // Verify all entities were created
        var permissions = await repo.GetAllPermissionsAsync(CancellationToken.None);
        var groups = await repo.GetAllGroupsAsync(CancellationToken.None);
        var users = await repo.GetAllUsersAsync(CancellationToken.None);

        Assert.True(permissions.Count >= 50);
        Assert.True(groups.Count >= 30);
        Assert.True(users.Count >= 100);

        // Concurrent permission assignments
        var assignmentTasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            var groupName = $"stress-group-{random.Next(30)}";
            var permName = $"stress-perm-{random.Next(50)}";
            var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
            
            assignmentTasks.Add(repo.SetGroupPermissionAsync(groupName, permName, access, CancellationToken.None));
        }

        for (int i = 0; i < 100; i++)
        {
            var email = $"stress-user-{random.Next(100)}@test.com";
            var permName = $"stress-perm-{random.Next(50)}";
            var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
            
            assignmentTasks.Add(repo.SetUserPermissionAsync(email, permName, access, CancellationToken.None));
        }

        await Task.WhenAll(assignmentTasks);

        // Concurrent permission calculations
        var calculationTasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            var email = $"stress-user-{random.Next(100)}@test.com";
            calculationTasks.Add(repo.CalculatePermissionsAsync(email, CancellationToken.None));
            calculationTasks.Add(repo.CalculatePermissionsDebugAsync(email, CancellationToken.None));
        }

        await Task.WhenAll(calculationTasks);
    }
}
