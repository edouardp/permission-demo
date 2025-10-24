using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/groups")]
#pragma warning disable S6960 // Controller responsibilities are focused on group management
public class GroupController(
    IPermissionsRepository repository,
    IHistoryService historyService,
    ILogger<GroupController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new group for organizing users and permissions
    /// </summary>
    /// <param name="request">Group details including name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created group with generated ID</returns>
    /// <response code="201">Group created successfully</response>
    /// <response code="400">Invalid request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Group), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        if (!GroupNameValidator.IsValid(request.Name))
        {
            return Problem(
                title: "Invalid Group Name",
                detail: GroupNameValidator.ValidationRules,
                statusCode: 400
            );
        }

        logger.LogInformation("Creating group {GroupName}", request.Name);
        var group = await repository.CreateGroupAsync(request.Name, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    /// <summary>
    /// Replaces all permissions for a group with the provided set (batch operation)
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="request">List of permissions with ALLOW/DENY access levels</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Group permissions updated successfully</response>
    /// <response code="400">Invalid permissions or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{groupId}/permissions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetGroupPermissions(string groupId, [FromBody] BatchPermissionRequest request, CancellationToken ct)
    {
        // Validate all permissions exist
        var allPermissions = request.Allow.Concat(request.Deny).Distinct().ToList();
        var invalidPermissions = new List<string>();
        
        foreach (var permissionName in allPermissions)
        {
            var permission = await repository.GetPermissionAsync(permissionName, ct);
            if (permission == null)
            {
                invalidPermissions.Add(permissionName);
            }
        }

        if (invalidPermissions.Count > 0)
        {
            logger.LogWarning("Invalid permissions for group {GroupId}: {InvalidPermissions}", groupId, string.Join(", ", invalidPermissions));
            return Problem(
                title: "Invalid Permissions",
                detail: $"The following permissions do not exist: {string.Join(", ", invalidPermissions)}",
                statusCode: 400
            );
        }

        // Replace all permissions for this group
        var permissions = new Dictionary<string, string>();
        foreach (var perm in request.Allow)
        {
            permissions[perm] = PermissionAccess.Allow;
        }
        foreach (var perm in request.Deny)
        {
            permissions[perm] = PermissionAccess.Deny;
        }
        
        await repository.SetGroupPermissionsAsync(groupId, permissions, ct, request.Principal, request.Reason);
        logger.LogInformation("Set {PermissionCount} permissions for group {GroupId}", permissions.Count, groupId);
        return Ok();
    }

    /// <summary>
    /// Sets a single permission for a group with ALLOW or DENY access
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="request">Access level (ALLOW or DENY)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Group permission set successfully</response>
    /// <response code="400">Invalid permission or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{groupId}/permissions/{permissionName}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetGroupPermission(string groupId, string permissionName, [FromBody] PermissionAccessRequest request, CancellationToken ct)
    {
        // Validate permission exists
        var permission = await repository.GetPermissionAsync(permissionName, ct);
        if (permission == null)
        {
            logger.LogWarning("Permission {PermissionName} not found", permissionName);
            return Problem(
                title: "Invalid Permission",
                detail: $"Permission '{permissionName}' does not exist",
                statusCode: 400
            );
        }

        await repository.SetGroupPermissionAsync(groupId, permissionName, request.Access, ct);
        logger.LogInformation("Set group {GroupId} permission {Permission} to {Access}", groupId, permissionName, request.Access);
        return Ok();
    }

    /// <summary>
    /// Removes a specific permission from a group
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="permissionName">Permission name to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Permission removed successfully</response>
    [HttpDelete("{groupId}/permissions/{permissionName}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveGroupPermission(string groupId, string permissionName, CancellationToken ct)
    {
        await repository.RemoveGroupPermissionAsync(groupId, permissionName, ct);
        logger.LogInformation("Removed group {GroupId} permission {Permission}", groupId, permissionName);
        return NoContent();
    }

    /// <summary>
    /// Gets change history for a specific group
    /// </summary>
    /// <param name="id">Group ID</param>
    /// <returns>List of historical changes for the group</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(List<HistoryEntry>), 200)]
    public async Task<IActionResult> GetGroupHistory(string id)
    {
        var history = await historyService.GetEntityHistoryAsync("Group", id);
        return Ok(history);
    }

    /// <summary>
    /// Deletes a group and all associated permissions
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Group deleted successfully</response>
    [HttpDelete("{groupId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteGroup(string groupId, CancellationToken ct)
    {
        logger.LogInformation("Deleting group {GroupId}", groupId);
        await repository.DeleteGroupAsync(groupId, ct);
        return NoContent();
    }
}
