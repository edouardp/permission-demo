using Microsoft.Extensions.Logging;
using PermissionsApi.Services;
using Xunit;

namespace PermissionsApi.UnitTests;

[Collection("MySQL")]
public class MySqlIntegrityCheckerTests(MySqlTestFixture fixture)
{
    private readonly MySqlTestFixture _fixture = fixture;

    private MySqlIntegrityChecker CreateChecker()
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MySqlIntegrityChecker>();
        return new MySqlIntegrityChecker(_fixture.ConnectionString, logger);
    }

    private MySqlPermissionsRepository CreateRepository()
    {
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repoLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MySqlPermissionsRepository>();
        return new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, repoLogger);
    }

    [Fact]
    public async Task GetPermissionDependenciesAsync_ShouldReturnEmptyForUnusedPermission()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var permissionName = $"unused-perm-{Guid.NewGuid()}";
        await repo.CreatePermissionAsync(permissionName, "Unused permission", false, CancellationToken.None);

        var dependencies = await checker.GetPermissionDependenciesAsync(permissionName);
        
        Assert.Equal(permissionName, dependencies.Permission);
        Assert.Empty(dependencies.Groups);
        Assert.Empty(dependencies.Users);
    }

    [Fact]
    public async Task GetPermissionDependenciesAsync_ShouldReturnGroupDependencies()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var permissionName = $"group-perm-{Guid.NewGuid()}";
        var groupName = $"test-group-{Guid.NewGuid()}";
        
        await repo.CreatePermissionAsync(permissionName, "Group permission", false, CancellationToken.None);
        await repo.CreateGroupAsync(groupName, CancellationToken.None);
        await repo.SetGroupPermissionAsync(groupName, permissionName, "ALLOW", CancellationToken.None);

        var dependencies = await checker.GetPermissionDependenciesAsync(permissionName);
        
        Assert.Equal(permissionName, dependencies.Permission);
        Assert.Single(dependencies.Groups);
        Assert.Contains(groupName, dependencies.Groups);
        Assert.Empty(dependencies.Users);
    }

    [Fact]
    public async Task GetPermissionDependenciesAsync_ShouldReturnUserDependencies()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var permissionName = $"user-perm-{Guid.NewGuid()}";
        var userEmail = $"test-{Guid.NewGuid()}@example.com";
        
        await repo.CreatePermissionAsync(permissionName, "User permission", false, CancellationToken.None);
        await repo.CreateUserAsync(userEmail, [], CancellationToken.None);
        await repo.SetUserPermissionAsync(userEmail, permissionName, "DENY", CancellationToken.None);

        var dependencies = await checker.GetPermissionDependenciesAsync(permissionName);
        
        Assert.Equal(permissionName, dependencies.Permission);
        Assert.Empty(dependencies.Groups);
        Assert.Single(dependencies.Users);
        Assert.Contains(userEmail, dependencies.Users);
    }

    [Fact]
    public async Task GetPermissionDependenciesAsync_ShouldReturnBothGroupAndUserDependencies()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var permissionName = $"mixed-perm-{Guid.NewGuid()}";
        var groupName = $"test-group-{Guid.NewGuid()}";
        var userEmail = $"test-{Guid.NewGuid()}@example.com";
        
        await repo.CreatePermissionAsync(permissionName, "Mixed permission", false, CancellationToken.None);
        await repo.CreateGroupAsync(groupName, CancellationToken.None);
        await repo.CreateUserAsync(userEmail, [], CancellationToken.None);
        await repo.SetGroupPermissionAsync(groupName, permissionName, "ALLOW", CancellationToken.None);
        await repo.SetUserPermissionAsync(userEmail, permissionName, "DENY", CancellationToken.None);

        var dependencies = await checker.GetPermissionDependenciesAsync(permissionName);
        
        Assert.Equal(permissionName, dependencies.Permission);
        Assert.Single(dependencies.Groups);
        Assert.Contains(groupName, dependencies.Groups);
        Assert.Single(dependencies.Users);
        Assert.Contains(userEmail, dependencies.Users);
    }

    [Fact]
    public async Task GetGroupDependenciesAsync_ShouldReturnEmptyForUnusedGroup()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var groupName = $"unused-group-{Guid.NewGuid()}";
        await repo.CreateGroupAsync(groupName, CancellationToken.None);

        var dependencies = await checker.GetGroupDependenciesAsync(groupName);
        
        Assert.Equal(groupName, dependencies.GroupName);
        Assert.Empty(dependencies.Users);
    }

    [Fact]
    public async Task GetGroupDependenciesAsync_ShouldReturnUserDependencies()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var groupName = $"used-group-{Guid.NewGuid()}";
        var userEmail = $"test-{Guid.NewGuid()}@example.com";
        
        await repo.CreateGroupAsync(groupName, CancellationToken.None);
        await repo.CreateUserAsync(userEmail, [groupName], CancellationToken.None);

        var dependencies = await checker.GetGroupDependenciesAsync(groupName);
        
        Assert.Equal(groupName, dependencies.GroupName);
        Assert.Single(dependencies.Users);
        Assert.Contains(userEmail, dependencies.Users);
    }

    [Fact]
    public async Task ScaleTest_ShouldHandleLargeDependencyChecks()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        var random = new Random(42);
        var testId = Guid.NewGuid().ToString("N")[..8];

        // Create 100 permissions
        var permissions = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            var permName = $"scale-perm-{testId}-{i}";
            permissions.Add(permName);
            await repo.CreatePermissionAsync(permName, $"Scale permission {i}", random.Next(2) == 0, CancellationToken.None);
        }

        // Create 50 groups
        var groups = new List<string>();
        for (int i = 0; i < 50; i++)
        {
            var groupName = $"scale-group-{testId}-{i}";
            groups.Add(groupName);
            await repo.CreateGroupAsync(groupName, CancellationToken.None);
            
            // Assign random permissions to each group
            var numPerms = random.Next(1, 10);
            for (int j = 0; j < numPerms; j++)
            {
                var perm = permissions[random.Next(permissions.Count)];
                var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                await repo.SetGroupPermissionAsync(groupName, perm, access, CancellationToken.None);
            }
        }

        // Create 200 users
        var users = new List<string>();
        for (int i = 0; i < 200; i++)
        {
            var userEmail = $"scale-user-{testId}-{i}@test.com";
            users.Add(userEmail);
            
            // Assign random groups to each user
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
            
            await repo.CreateUserAsync(userEmail, userGroups, CancellationToken.None);
            
            // Assign random direct permissions to some users
            if (random.Next(3) == 0) // 1/3 of users get direct permissions
            {
                var numPerms = random.Next(1, 5);
                for (int j = 0; j < numPerms; j++)
                {
                    var perm = permissions[random.Next(permissions.Count)];
                    var access = random.Next(2) == 0 ? "ALLOW" : "DENY";
                    await repo.SetUserPermissionAsync(userEmail, perm, access, CancellationToken.None);
                }
            }
        }

        // Test dependency checking for all permissions
        var permissionTasks = permissions.Select(async perm =>
        {
            var deps = await checker.GetPermissionDependenciesAsync(perm);
            Assert.Equal(perm, deps.Permission);
            Assert.True(deps.Groups.Count <= groups.Count);
            Assert.True(deps.Users.Count <= users.Count);
            return deps;
        });

        var permissionDeps = await Task.WhenAll(permissionTasks);

        // Test dependency checking for all groups
        var groupTasks = groups.Select(async group =>
        {
            var deps = await checker.GetGroupDependenciesAsync(group);
            Assert.Equal(group, deps.GroupName);
            Assert.True(deps.Users.Count <= users.Count);
            return deps;
        });

        var groupDeps = await Task.WhenAll(groupTasks);

        // Verify some permissions have dependencies
        Assert.True(permissionDeps.Any(d => d.Groups.Count > 0 || d.Users.Count > 0));
        
        // Verify some groups have user dependencies
        Assert.True(groupDeps.Any(d => d.Users.Count > 0));

        // Test concurrent dependency checks
        var concurrentTasks = new List<Task>();
        for (int i = 0; i < 50; i++)
        {
            var perm = permissions[random.Next(permissions.Count)];
            var group = groups[random.Next(groups.Count)];
            
            concurrentTasks.Add(checker.GetPermissionDependenciesAsync(perm));
            concurrentTasks.Add(checker.GetGroupDependenciesAsync(group));
        }

        await Task.WhenAll(concurrentTasks);
    }

    [Fact]
    public async Task GetPermissionDependenciesAsync_ShouldReturnSortedResults()
    {
        var checker = CreateChecker();
        var repo = CreateRepository();
        
        var permissionName = $"sorted-perm-{Guid.NewGuid()}";
        await repo.CreatePermissionAsync(permissionName, "Sorted permission", false, CancellationToken.None);
        
        // Create groups and users with names that will test sorting
        var groupNames = new[] { "zebra-group", "alpha-group", "beta-group" };
        var userEmails = new[] { "zulu@test.com", "alpha@test.com", "bravo@test.com" };
        
        foreach (var groupName in groupNames)
        {
            await repo.CreateGroupAsync(groupName, CancellationToken.None);
            await repo.SetGroupPermissionAsync(groupName, permissionName, "ALLOW", CancellationToken.None);
        }
        
        foreach (var userEmail in userEmails)
        {
            await repo.CreateUserAsync(userEmail, [], CancellationToken.None);
            await repo.SetUserPermissionAsync(userEmail, permissionName, "DENY", CancellationToken.None);
        }

        var dependencies = await checker.GetPermissionDependenciesAsync(permissionName);
        
        // Verify groups are sorted alphabetically
        Assert.Equal(groupNames.OrderBy(x => x).ToList(), dependencies.Groups);
        
        // Verify users are sorted alphabetically
        Assert.Equal(userEmails.OrderBy(x => x).ToList(), dependencies.Users);
    }
}
