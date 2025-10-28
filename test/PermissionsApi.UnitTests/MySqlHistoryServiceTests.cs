using Microsoft.Extensions.Logging;
using PermissionsApi.Models;
using PermissionsApi.Services;
using Xunit;

namespace PermissionsApi.UnitTests;

[Collection("MySQL")]
public class MySqlHistoryServiceTests(MySqlTestFixture fixture)
{
    private readonly MySqlTestFixture _fixture = fixture;

    private MySqlHistoryService CreateService()
    {
        return new MySqlHistoryService(_fixture.ConnectionString, TimeProvider.System);
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldRecordPermissionChange()
    {
        var service = CreateService();
        var permission = new Permission { Name = "test-perm", Description = "Test", IsDefault = false };

        await service.RecordChangeAsync("CREATE", "Permission", "test-perm", permission, "admin", "test");

        var history = await service.GetEntityHistoryAsync("Permission", "test-perm");
        
        Assert.Single(history);
        Assert.Equal("CREATE", history[0].ChangeType);
        Assert.Equal("Permission", history[0].EntityType);
        Assert.Equal("test-perm", history[0].EntityId);
        Assert.Equal("admin", history[0].Principal);
        Assert.Equal("test", history[0].Reason);
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldRecordGroupChange()
    {
        var service = CreateService();
        var group = new Group { Name = "test-group", Permissions = new Dictionary<string, string> { ["read"] = "ALLOW" } };

        await service.RecordChangeAsync("UPDATE", "Group", "test-group", group, "admin", "test");

        var history = await service.GetEntityHistoryAsync("Group", "test-group");
        
        Assert.Single(history);
        Assert.Equal("UPDATE", history[0].ChangeType);
        Assert.Equal("Group", history[0].EntityType);
        Assert.Equal("test-group", history[0].EntityId);
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldRecordUserChange()
    {
        var service = CreateService();
        var user = new User { Email = "test@example.com", Groups = ["group1"], Permissions = new Dictionary<string, string> { ["write"] = "DENY" } };

        await service.RecordChangeAsync("DELETE", "User", "test@example.com", user);

        var history = await service.GetEntityHistoryAsync("User", "test@example.com");
        
        Assert.Single(history);
        Assert.Equal("DELETE", history[0].ChangeType);
        Assert.Equal("User", history[0].EntityType);
        Assert.Equal("test@example.com", history[0].EntityId);
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldReturnAllChanges()
    {
        var service = CreateService();
        
        // Record multiple changes
        await service.RecordChangeAsync("CREATE", "Permission", "perm1", new Permission { Name = "perm1", Description = "Test" });
        await service.RecordChangeAsync("CREATE", "Group", "group1", new Group { Name = "group1" });
        await service.RecordChangeAsync("CREATE", "User", "user1@test.com", new User { Email = "user1@test.com" });

        var history = await service.GetHistoryAsync();
        
        Assert.True(history.Count >= 3);
        Assert.Contains(history, h => h.EntityType == "Permission" && h.EntityId == "perm1");
        Assert.Contains(history, h => h.EntityType == "Group" && h.EntityId == "group1");
        Assert.Contains(history, h => h.EntityType == "User" && h.EntityId == "user1@test.com");
    }

    [Fact]
    public async Task GetHistoryAsync_ShouldSupportPagination()
    {
        var service = CreateService();
        var entityId = Guid.NewGuid().ToString();
        
        // Record 15 changes
        for (int i = 0; i < 15; i++)
        {
            await service.RecordChangeAsync("UPDATE", "Permission", entityId, new Permission { Name = entityId, Description = "Test" }, $"user{i}", $"reason{i}");
        }

        var page1 = await service.GetHistoryAsync(0, 10);
        var page2 = await service.GetHistoryAsync(10, 10);
        
        Assert.Equal(10, page1.Count);
        Assert.True(page2.Count >= 5);
    }

    [Fact]
    public async Task RecordChangeAsync_ShouldHandleNullPrincipalAndReason()
    {
        var service = CreateService();
        var permission = new Permission { Name = "test-null", Description = "Test", IsDefault = false };

        await service.RecordChangeAsync("CREATE", "Permission", "test-null", permission);

        var history = await service.GetEntityHistoryAsync("Permission", "test-null");
        
        Assert.Single(history);
        Assert.Null(history[0].Principal);
        Assert.Null(history[0].Reason);
    }

    [Fact]
    public async Task ScaleTest_ShouldHandle1000HistoryEntries()
    {
        var service = CreateService();
        var random = new Random(42); // Fixed seed for reproducibility
        var entityTypes = new[] { "Permission", "Group", "User" };
        var actions = new[] { "CREATE", "UPDATE", "DELETE" };

        // Record 1000 history entries
        var tasks = new List<Task>();
        for (int i = 0; i < 1000; i++)
        {
            var entityType = entityTypes[random.Next(entityTypes.Length)];
            var action = actions[random.Next(actions.Length)];
            var entityId = $"{entityType.ToLower()}-{i}";
            
            IEntity entity = entityType switch
            {
                "Permission" => new Permission { Name = entityId, Description = $"Description {i}" },
                "Group" => new Group { Name = entityId, Permissions = new Dictionary<string, string>() },
                "User" => new User { Email = $"{entityId}@test.com", Groups = [] },
                _ => new Permission { Name = entityId, Description = "Default" }
            };

            tasks.Add(service.RecordChangeAsync(action, entityType, entityId, entity, $"user{i % 10}", $"reason{i}"));
        }

        await Task.WhenAll(tasks);

        // Verify we can retrieve the history
        var globalHistory = await service.GetHistoryAsync(0, 100);
        Assert.Equal(100, globalHistory.Count);

        // Test pagination through all entries
        var allEntries = new List<HistoryEntry>();
        int skip = 0;
        const int pageSize = 50;
        
        while (true)
        {
            var page = await service.GetHistoryAsync(skip, pageSize);
            if (page.Count == 0) break;
            
            allEntries.AddRange(page);
            skip += pageSize;
        }

        Assert.True(allEntries.Count >= 1000);
        
        // Verify entries are ordered by timestamp (most recent first)
        for (int i = 1; i < Math.Min(100, allEntries.Count); i++)
        {
            Assert.True(allEntries[i-1].TimestampUtc >= allEntries[i].TimestampUtc);
        }
    }
}
