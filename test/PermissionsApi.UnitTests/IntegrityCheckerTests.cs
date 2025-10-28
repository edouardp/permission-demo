using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using PermissionsApi.Services;

namespace PermissionsApi.UnitTests;

public class IntegrityCheckerTests
{
    private readonly PermissionsRepository repository;
    private readonly IntegrityChecker checker;

    public IntegrityCheckerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
        var repoLogger = loggerFactory.CreateLogger<PermissionsRepository>();
        var historyLogger = loggerFactory.CreateLogger<HistoryService>();
        var checkerLogger = loggerFactory.CreateLogger<IntegrityChecker>();
        
        var historyService = new HistoryService(TimeProvider.System, historyLogger);
        repository = new PermissionsRepository(repoLogger, historyService);
        checker = new IntegrityChecker(repository, checkerLogger);
    }

    [Fact]
    public async Task CanDeletePermission_NoReferences_ReturnsValid()
    {
        await repository.CreatePermissionAsync("read", "Read access", false, CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("read");

        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task CanDeletePermission_UsedByGroup_ReturnsInvalid()
    {
        await repository.CreatePermissionAsync("write", "Write access", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("editors", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "write", "ALLOW", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("write");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("editors");
        result.Reason.Should().Contain("groups");
    }

    [Fact]
    public async Task CanDeletePermission_UsedByMultipleGroups_ListsAllGroups()
    {
        await repository.CreatePermissionAsync("delete", "Delete access", false, CancellationToken.None);
        var group1 = await repository.CreateGroupAsync("admins", CancellationToken.None);
        var group2 = await repository.CreateGroupAsync("superusers", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group1.Id, "delete", "ALLOW", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group2.Id, "delete", "ALLOW", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("delete");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("admins");
        result.Reason.Should().Contain("superusers");
    }

    [Fact]
    public async Task CanDeletePermission_UsedByUser_ReturnsInvalid()
    {
        await repository.CreatePermissionAsync("admin", "Admin access", false, CancellationToken.None);
        await repository.CreateUserAsync("user@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("user@example.com", "admin", "ALLOW", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("admin");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("user@example.com");
        result.Reason.Should().Contain("users");
    }

    [Fact]
    public async Task CanDeletePermission_UsedByMultipleUsers_ListsAllUsers()
    {
        await repository.CreatePermissionAsync("special", "Special access", false, CancellationToken.None);
        await repository.CreateUserAsync("alice@example.com", [], CancellationToken.None);
        await repository.CreateUserAsync("bob@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("alice@example.com", "special", "ALLOW", CancellationToken.None);
        await repository.SetUserPermissionAsync("bob@example.com", "special", "DENY", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("special");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("alice@example.com");
        result.Reason.Should().Contain("bob@example.com");
    }

    [Fact]
    public async Task CanDeletePermission_UsedByGroupAndUser_ReportsGroupFirst()
    {
        await repository.CreatePermissionAsync("mixed", "Mixed usage", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("team", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "mixed", "ALLOW", CancellationToken.None);
        await repository.CreateUserAsync("user@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("user@example.com", "mixed", "DENY", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("mixed");

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("team");
        result.Reason.Should().NotContain("user@example.com");
    }

    [Fact]
    public async Task CanDeletePermission_PermissionRemovedFromGroup_ReturnsValid()
    {
        await repository.CreatePermissionAsync("temp", "Temporary", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("test-group", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "temp", "ALLOW", CancellationToken.None);
        await repository.RemoveGroupPermissionAsync(group.Name, "temp", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("temp");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeletePermission_PermissionRemovedFromUser_ReturnsValid()
    {
        await repository.CreatePermissionAsync("temp2", "Temporary 2", false, CancellationToken.None);
        await repository.CreateUserAsync("temp@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("temp@example.com", "temp2", "ALLOW", CancellationToken.None);
        await repository.RemoveUserPermissionAsync("temp@example.com", "temp2", CancellationToken.None);

        var result = await checker.CanDeletePermissionAsync("temp2");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeletePermission_NonExistentPermission_ReturnsValid()
    {
        var result = await checker.CanDeletePermissionAsync("nonexistent");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteGroup_NoUsers_ReturnsValid()
    {
        var group = await repository.CreateGroupAsync("empty-group", CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group.Name);

        result.IsValid.Should().BeTrue();
        result.Reason.Should().BeNull();
    }

    [Fact]
    public async Task CanDeleteGroup_HasOneUser_ReturnsInvalid()
    {
        var group = await repository.CreateGroupAsync("active-group", CancellationToken.None);
        await repository.CreateUserAsync("member@example.com", [group.Name], CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group.Name);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("member@example.com");
        result.Reason.Should().Contain("users");
    }

    [Fact]
    public async Task CanDeleteGroup_HasMultipleUsers_ListsAllUsers()
    {
        var group = await repository.CreateGroupAsync("popular-group", CancellationToken.None);
        await repository.CreateUserAsync("user1@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("user2@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("user3@example.com", [group.Name], CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group.Name);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("user1@example.com");
        result.Reason.Should().Contain("user2@example.com");
        result.Reason.Should().Contain("user3@example.com");
    }

    [Fact]
    public async Task CanDeleteGroup_UserInMultipleGroups_StillReportsUser()
    {
        var group1 = await repository.CreateGroupAsync("group1", CancellationToken.None);
        var group2 = await repository.CreateGroupAsync("group2", CancellationToken.None);
        await repository.CreateUserAsync("multi@example.com", [group1.Id, group2.Id], CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group1.Id);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("multi@example.com");
    }

    [Fact]
    public async Task CanDeleteGroup_NonExistentGroup_ReturnsValid()
    {
        var result = await checker.CanDeleteGroupAsync("nonexistent-group-id");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteGroup_UserDeleted_ReturnsValid()
    {
        var group = await repository.CreateGroupAsync("temp-group", CancellationToken.None);
        await repository.CreateUserAsync("temp-user@example.com", [group.Name], CancellationToken.None);
        await repository.DeleteUserAsync("temp-user@example.com", CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group.Name);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CanDeleteGroup_GroupHasPermissions_StillValidIfNoUsers()
    {
        await repository.CreatePermissionAsync("perm1", "Permission 1", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("perm-group", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "perm1", "ALLOW", CancellationToken.None);

        var result = await checker.CanDeleteGroupAsync(group.Name);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task IntegrityCheck_ComplexScenario_ValidatesCorrectly()
    {
        await repository.CreatePermissionAsync("complex", "Complex perm", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("complex-group", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "complex", "ALLOW", CancellationToken.None);
        await repository.CreateUserAsync("user1@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("user2@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("user2@example.com", "complex", "DENY", CancellationToken.None);

        var permResult = await checker.CanDeletePermissionAsync("complex");
        permResult.IsValid.Should().BeFalse();
        permResult.Reason.Should().Contain("complex-group");

        var groupResult = await checker.CanDeleteGroupAsync(group.Name);
        groupResult.IsValid.Should().BeFalse();
        groupResult.Reason.Should().Contain("user1@example.com");

        await repository.DeleteUserAsync("user1@example.com", CancellationToken.None);
        groupResult = await checker.CanDeleteGroupAsync(group.Name);
        groupResult.IsValid.Should().BeTrue();

        await repository.RemoveGroupPermissionAsync(group.Name, "complex", CancellationToken.None);
        permResult = await checker.CanDeletePermissionAsync("complex");
        permResult.IsValid.Should().BeFalse();
        permResult.Reason.Should().Contain("user2@example.com");

        await repository.RemoveUserPermissionAsync("user2@example.com", "complex", CancellationToken.None);
        permResult = await checker.CanDeletePermissionAsync("complex");
        permResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetPermissionDependencies_NoDependencies_ReturnsEmptyLists()
    {
        await repository.CreatePermissionAsync("unused", "Unused permission", false, CancellationToken.None);

        var deps = await checker.GetPermissionDependenciesAsync("unused");

        deps.Permission.Should().Be("unused");
        deps.Groups.Should().BeEmpty();
        deps.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionDependencies_UsedByGroups_ReturnsGroupNames()
    {
        await repository.CreatePermissionAsync("shared", "Shared permission", false, CancellationToken.None);
        var group1 = await repository.CreateGroupAsync("team-a", CancellationToken.None);
        var group2 = await repository.CreateGroupAsync("team-b", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group1.Id, "shared", "ALLOW", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group2.Id, "shared", "DENY", CancellationToken.None);

        var deps = await checker.GetPermissionDependenciesAsync("shared");

        deps.Groups.Should().BeEquivalentTo("team-a", "team-b");
        deps.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionDependencies_UsedByUsers_ReturnsUserEmails()
    {
        await repository.CreatePermissionAsync("personal", "Personal permission", false, CancellationToken.None);
        await repository.CreateUserAsync("alice@example.com", [], CancellationToken.None);
        await repository.CreateUserAsync("bob@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("alice@example.com", "personal", "ALLOW", CancellationToken.None);
        await repository.SetUserPermissionAsync("bob@example.com", "personal", "ALLOW", CancellationToken.None);

        var deps = await checker.GetPermissionDependenciesAsync("personal");

        deps.Groups.Should().BeEmpty();
        deps.Users.Should().BeEquivalentTo("alice@example.com", "bob@example.com");
    }

    [Fact]
    public async Task GetPermissionDependencies_UsedByBoth_ReturnsBothLists()
    {
        await repository.CreatePermissionAsync("mixed", "Mixed usage", false, CancellationToken.None);
        var group = await repository.CreateGroupAsync("mixed-group", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group.Name, "mixed", "ALLOW", CancellationToken.None);
        await repository.CreateUserAsync("user@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("user@example.com", "mixed", "DENY", CancellationToken.None);

        var deps = await checker.GetPermissionDependenciesAsync("mixed");

        deps.Groups.Should().ContainSingle().Which.Should().Be("mixed-group");
        deps.Users.Should().ContainSingle().Which.Should().Be("user@example.com");
    }

    [Fact]
    public async Task GetPermissionDependencies_NonExistent_ReturnsEmptyLists()
    {
        var deps = await checker.GetPermissionDependenciesAsync("nonexistent");

        deps.Permission.Should().Be("nonexistent");
        deps.Groups.Should().BeEmpty();
        deps.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPermissionDependencies_ResultsAreSorted()
    {
        await repository.CreatePermissionAsync("sorted", "Sorted test", false, CancellationToken.None);
        var group1 = await repository.CreateGroupAsync("zebra", CancellationToken.None);
        var group2 = await repository.CreateGroupAsync("alpha", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group1.Id, "sorted", "ALLOW", CancellationToken.None);
        await repository.SetGroupPermissionAsync(group2.Id, "sorted", "ALLOW", CancellationToken.None);
        await repository.CreateUserAsync("zoe@example.com", [], CancellationToken.None);
        await repository.CreateUserAsync("adam@example.com", [], CancellationToken.None);
        await repository.SetUserPermissionAsync("zoe@example.com", "sorted", "ALLOW", CancellationToken.None);
        await repository.SetUserPermissionAsync("adam@example.com", "sorted", "ALLOW", CancellationToken.None);

        var deps = await checker.GetPermissionDependenciesAsync("sorted");

        deps.Groups.Should().Equal("alpha", "zebra");
        deps.Users.Should().Equal("adam@example.com", "zoe@example.com");
    }

    [Fact]
    public async Task GetGroupDependencies_NoUsers_ReturnsEmptyList()
    {
        var group = await repository.CreateGroupAsync("empty", CancellationToken.None);

        var deps = await checker.GetGroupDependenciesAsync(group.Name);

        deps.GroupName.Should().Be("empty");
        deps.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupDependencies_HasUsers_ReturnsUserEmails()
    {
        var group = await repository.CreateGroupAsync("active", CancellationToken.None);
        await repository.CreateUserAsync("member1@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("member2@example.com", [group.Name], CancellationToken.None);

        var deps = await checker.GetGroupDependenciesAsync(group.Name);

        deps.GroupName.Should().Be("active");
        deps.Users.Should().BeEquivalentTo("member1@example.com", "member2@example.com");
    }

    [Fact]
    public async Task GetGroupDependencies_NonExistent_ReturnsGroupName()
    {
        var deps = await checker.GetGroupDependenciesAsync("nonexistent-group");

        deps.GroupName.Should().Be("nonexistent-group");
        deps.Users.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupDependencies_ResultsAreSorted()
    {
        var group = await repository.CreateGroupAsync("sorted-group", CancellationToken.None);
        await repository.CreateUserAsync("zara@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("alex@example.com", [group.Name], CancellationToken.None);
        await repository.CreateUserAsync("mike@example.com", [group.Name], CancellationToken.None);

        var deps = await checker.GetGroupDependenciesAsync(group.Name);

        deps.Users.Should().Equal("alex@example.com", "mike@example.com", "zara@example.com");
    }
}
