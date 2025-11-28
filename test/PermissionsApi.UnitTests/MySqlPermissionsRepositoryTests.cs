using Microsoft.Extensions.Logging;
using PermissionsApi.Services;
using PermissionsApi.TestSupport;
using Xunit;

namespace PermissionsApi.UnitTests;

[Collection("MySQL")]
public class MySqlPermissionsRepositoryTests(MySqlTestFixture fixture)
{
    private readonly MySqlTestFixture _fixture = fixture;

    [Fact]
    public async Task CreatePermissionAsync_ShouldCreatePermission()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<MySqlPermissionsRepository>();
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repository = new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, logger);
        
        var permissionName = $"test-permission-{Guid.NewGuid()}";
        
        // Act
        var permission = await repository.CreatePermissionAsync(
            permissionName, 
            "Test permission", 
            false, 
            CancellationToken.None);
        
        // Assert
        Assert.Equal(permissionName, permission.Name);
        Assert.Equal("Test permission", permission.Description);
        Assert.False(permission.IsDefault);
        
        // Verify it can be retrieved
        var retrieved = await repository.GetPermissionAsync(permissionName, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(permissionName, retrieved.Name);
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldCreateGroup()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<MySqlPermissionsRepository>();
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repository = new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, logger);
        
        var groupName = $"test-group-{Guid.NewGuid()}";
        
        // Act
        var group = await repository.CreateGroupAsync(groupName, CancellationToken.None);
        
        // Assert
        Assert.Equal(groupName, group.Name);
        Assert.Empty(group.Permissions);
        
        // Verify it can be retrieved
        var retrieved = await repository.GetGroupAsync(groupName, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(groupName, retrieved.Name);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldCreateUser()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<MySqlPermissionsRepository>();
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repository = new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, logger);
        
        var email = $"test-{Guid.NewGuid()}@example.com";
        var groups = new List<string>();
        
        // Act
        var user = await repository.CreateUserAsync(email, groups, CancellationToken.None);
        
        // Assert
        Assert.Equal(email, user.Email);
        Assert.Empty(user.Groups);
        Assert.Empty(user.Permissions);
        
        // Verify it can be retrieved
        var retrieved = await repository.GetUserAsync(email, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(email, retrieved.Email);
    }

    [Fact]
    public async Task CalculatePermissionsAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var logger = new LoggerFactory().CreateLogger<MySqlPermissionsRepository>();
        var historyService = new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
        var repository = new MySqlPermissionsRepository(_fixture.ConnectionString, historyService, logger);
        
        var permissionName = $"test-perm-{Guid.NewGuid()}";
        var groupName = $"test-group-{Guid.NewGuid()}";
        var email = $"test-{Guid.NewGuid()}@example.com";
        
        // Create permission with default = true
        await repository.CreatePermissionAsync(permissionName, "Test permission", true, CancellationToken.None);
        
        // Create group and user
        await repository.CreateGroupAsync(groupName, CancellationToken.None);
        await repository.CreateUserAsync(email, [groupName], CancellationToken.None);
        
        // Act
        var permissions = await repository.CalculatePermissionsAsync(email, CancellationToken.None);
        
        // Assert
        Assert.NotNull(permissions);
        Assert.True(permissions.ContainsKey(permissionName));
        Assert.True(permissions[permissionName]); // Should be true due to default
    }
}
