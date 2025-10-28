using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/groups")]
#pragma warning disable S6960 // Controller responsibilities are focused on group management
public class GroupController(
    IPermissionsRepository repository,
    IHistoryService historyService,
    IIntegrityChecker integrityChecker,
    ILogger<GroupController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new group for organizing users and permissions
    /// </summary>
    /// <param name="request">Group details including name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created group</returns>
    /// <response code="201">Group created successfully</response>
    /// <response code="400">Invalid request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Group), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        try
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
            return CreatedAtAction(nameof(CreateGroup), new { name = group.Name });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create group {GroupName}", request.Name);
            throw new OperationException("Operation failed", ex);
        }
    }

    /// <summary>
    /// Replaces all permissions for a group with the provided set (batch operation)
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="request">List of permissions with ALLOW/DENY access levels</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Group permissions updated successfully</response>
    /// <response code="400">Invalid permissions or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{groupName}/permissions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetGroupPermissions(string groupName, [FromBody] BatchPermissionRequest request, CancellationToken ct)
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
            logger.LogWarning("Invalid permissions for group {GroupName}: {InvalidPermissions}", groupName, string.Join(", ", invalidPermissions));
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
        
        await repository.SetGroupPermissionsAsync(groupName, permissions, ct, request.Principal, request.Reason);
        logger.LogInformation("Set {PermissionCount} permissions for group {GroupName}", permissions.Count, groupName);
        return Ok();
    }

    /// <summary>
    /// Sets a single permission for a group with ALLOW or DENY access
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="request">Access level (ALLOW or DENY)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Group permission set successfully</response>
    /// <response code="400">Invalid permission or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{groupName}/permissions/{permissionName}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetGroupPermission(string groupName, string permissionName, [FromBody] PermissionAccessRequest request, CancellationToken ct)
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

        await repository.SetGroupPermissionAsync(groupName, permissionName, request.Access, ct);
        logger.LogInformation("Set group {GroupName} permission {Permission} to {Access}", groupName, permissionName, request.Access);
        return Ok();
    }

    /// <summary>
    /// Removes a specific permission from a group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="permissionName">Permission name to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Permission removed successfully</response>
    [HttpDelete("{groupName}/permissions/{permissionName}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveGroupPermission(string groupName, string permissionName, CancellationToken ct)
    {
        await repository.RemoveGroupPermissionAsync(groupName, permissionName, ct);
        logger.LogInformation("Removed group {GroupName} permission {Permission}", groupName, permissionName);
        return NoContent();
    }

    /// <summary>
    /// Gets change history for a specific group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <returns>List of historical changes for the group</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet("{groupName}/history")]
    [ProducesResponseType(typeof(List<HistoryEntry>), 200)]
    public async Task<IActionResult> GetGroupHistory(string groupName)
    {
        var history = await historyService.GetEntityHistoryAsync("Group", groupName);
        return Ok(history);
    }

    /// <summary>
    /// Gets dependencies that would prevent deletion of a group
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <returns>List of users in this group</returns>
    /// <response code="200">Dependencies retrieved successfully</response>
    [HttpGet("{groupName}/dependencies")]
    [ProducesResponseType(typeof(GroupDependencies), 200)]
    public async Task<IActionResult> GetGroupDependencies(string groupName)
    {
        var dependencies = await integrityChecker.GetGroupDependenciesAsync(groupName);
        return Ok(dependencies);
    }

    /// <summary>
    /// Deletes a group if not assigned to any users
    /// </summary>
    /// <param name="groupName">Group name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Group deleted successfully</response>
    /// <response code="409">Group is assigned to users. Response is RFC 9457 Problem Details JSON.</response>
    [HttpDelete("{groupName}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteGroup(string groupName, CancellationToken ct)
    {
        logger.LogInformation("Deleting group {GroupName}", groupName);
        
        var integrityCheck = await integrityChecker.CanDeleteGroupAsync(groupName);
        if (!integrityCheck.IsValid)
        {
            logger.LogWarning("Cannot delete group {GroupName}: {Reason}", groupName, integrityCheck.Reason);
            return Conflict(new ProblemDetails
            {
                Title = "Referential integrity violation",
                Detail = integrityCheck.Reason,
                Status = 409
            });
        }
        
        await repository.DeleteGroupAsync(groupName, ct);
        return NoContent();
    }
}
