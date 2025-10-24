using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1")]
#pragma warning disable S6960 // Controller responsibilities are appropriate for this API size
public class PermissionsController(
    IPermissionsRepository repository,
    IHistoryService historyService,
    ILogger<PermissionsController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new permission with optional default status
    /// </summary>
    /// <param name="request">Permission details including name, description, and default status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created permission</returns>
    /// <response code="201">Permission created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost("permissions")]
    [ProducesResponseType(typeof(Permission), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating permission {PermissionName} (IsDefault: {IsDefault})", request.Name, request.IsDefault);
        var permission = await repository.CreatePermissionAsync(request.Name, request.Description, request.IsDefault, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(GetPermission), new { name = permission.Name }, permission);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var permissions = await repository.GetAllPermissionsAsync(ct);
        return Ok(permissions);
    }

    [HttpGet("permissions/{name}")]
    public async Task<IActionResult> GetPermission(string name, CancellationToken ct)
    {
        var permission = await repository.GetPermissionAsync(name, ct);
        if (permission == null)
        {
            logger.LogWarning("Permission {PermissionName} not found", name);
            return NotFound();
        }
        return Ok(permission);
    }

    [HttpPut("permissions/{name}")]
    public async Task<IActionResult> UpdatePermission(string name, [FromBody] UpdatePermissionRequest request, CancellationToken ct)
    {
        logger.LogInformation("Updating permission {PermissionName}", name);
        if (!await repository.UpdatePermissionAsync(name, request.Description, ct, request.Principal, request.Reason))
        {
            logger.LogWarning("Permission {PermissionName} not found for update", name);
            return NotFound();
        }
        return Ok();
    }

    [HttpDelete("permissions/{name}")]
    public async Task<IActionResult> DeletePermission(string name, CancellationToken ct)
    {
        logger.LogInformation("Deleting permission {PermissionName}", name);
        if (!await repository.DeletePermissionAsync(name, ct))
        {
            logger.LogWarning("Permission {PermissionName} not found for deletion", name);
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("permissions/{name}/default")]
    public async Task<IActionResult> SetPermissionDefault(string name, [FromBody] bool isDefault, CancellationToken ct)
    {
        logger.LogInformation("Setting permission {PermissionName} IsDefault to {IsDefault}", name, isDefault);
        if (!await repository.SetPermissionDefaultAsync(name, isDefault, ct))
        {
            logger.LogWarning("Permission {PermissionName} not found", name);
            return NotFound();
        }
        return Ok();
    }

    /// <summary>
    /// Gets calculated permissions for a user, showing final allow/deny status after applying hierarchy
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User's calculated permissions with allow and deny lists</returns>
    /// <response code="200">Permissions calculated successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("users/{email}/permissions")]
    [ProducesResponseType(typeof(PermissionsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPermissions(string email, CancellationToken ct)
    {
        var permissions = await repository.CalculatePermissionsAsync(email, ct);
        if (permissions == null)
        {
            logger.LogWarning("User {Email} not found", email);
            return NotFound();
        }

        var response = new PermissionsResponse 
        { 
            Email = email,
            Allow = permissions.Where(p => p.Value).Select(p => p.Key).OrderBy(p => p).ToList(),
            Deny = permissions.Where(p => !p.Value).Select(p => p.Key).OrderBy(p => p).ToList()
        };
        
        return Ok(response);
    }

    /// <summary>
    /// Debug permission resolution chain showing how each permission is calculated through Default → Group → User hierarchy
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed permission resolution chain for debugging</returns>
    /// <response code="200">Debug information retrieved successfully</response>
    /// <response code="404">User not found</response>
    [HttpGet("user/{email}/debug")]
    [ProducesResponseType(typeof(PermissionDebugResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserPermissionsDebug(string email, CancellationToken ct)
    {
        var debug = await repository.CalculatePermissionsDebugAsync(email, ct);
        if (debug == null)
        {
            return NotFound();
        }

        return Ok(debug);
    }

    [HttpGet("permissions/{name}/history")]
    public async Task<IActionResult> GetPermissionHistory(string name)
    {
        var history = await historyService.GetEntityHistoryAsync("Permission", name);
        return Ok(history);
    }

    [HttpGet("users/{email}/history")]
    public async Task<IActionResult> GetUserHistory(string email)
    {
        var history = await historyService.GetEntityHistoryAsync("User", email);
        return Ok(history);
    }

    [HttpGet("groups/{id}/history")]
    public async Task<IActionResult> GetGroupHistory(string id)
    {
        var history = await historyService.GetEntityHistoryAsync("Group", id);
        return Ok(history);
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating group {GroupName}", request.Name);
        var group = await repository.CreateGroupAsync(request.Name, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    [HttpPut("groups/{groupId}/permissions")]
    public async Task<IActionResult> SetGroupPermissions(string groupId, [FromBody] BatchPermissionRequest request, CancellationToken ct)
    {
        // Validate all permissions exist
        var permissionNames = request.Permissions.Select(permissionRequest => permissionRequest.Permission).ToList();
        var invalidPermissions = new List<string>();
        
        foreach (var permissionName in permissionNames)
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
        await repository.ReplaceGroupPermissionsAsync(groupId, request.Permissions, ct);

        logger.LogInformation("Replaced permissions for group {GroupId} with {Count} permissions", groupId, request.Permissions.Count);
        return Ok();
    }

    [HttpPut("groups/{groupId}/permissions/{permissionName}")]
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

    [HttpDelete("groups/{groupId}/permissions/{permissionName}")]
    public async Task<IActionResult> RemoveGroupPermission(string groupId, string permissionName, CancellationToken ct)
    {
        await repository.RemoveGroupPermissionAsync(groupId, permissionName, ct);
        logger.LogInformation("Removed group {GroupId} permission {Permission}", groupId, permissionName);
        return NoContent();
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        logger.LogInformation("Creating user {Email}", request.Email);
        await repository.CreateUserAsync(request.Email, request.Groups, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    [HttpPut("users/{email}/permissions")]
    public async Task<IActionResult> SetUserPermissions(string email, [FromBody] BatchPermissionRequest request, CancellationToken ct)
    {
        // Validate all permissions exist
        var permissionNames = request.Permissions.Select(permissionRequest => permissionRequest.Permission).ToList();
        var invalidPermissions = new List<string>();
        
        foreach (var permissionName in permissionNames)
        {
            var permission = await repository.GetPermissionAsync(permissionName, ct);
            if (permission == null)
            {
                invalidPermissions.Add(permissionName);
            }
        }

        if (invalidPermissions.Count > 0)
        {
            logger.LogWarning("Invalid permissions for user {Email}: {InvalidPermissions}", email, string.Join(", ", invalidPermissions));
            return Problem(
                title: "Invalid Permissions",
                detail: $"The following permissions do not exist: {string.Join(", ", invalidPermissions)}",
                statusCode: 400
            );
        }

        // Replace all permissions for this user
        await repository.ReplaceUserPermissionsAsync(email, request.Permissions, ct);

        logger.LogInformation("Replaced permissions for user {Email} with {Count} permissions", email, request.Permissions.Count);
        return Ok();
    }

    [HttpPut("users/{email}/permissions/{permissionName}")]
    public async Task<IActionResult> SetUserPermission(string email, string permissionName, [FromBody] PermissionAccessRequest request, CancellationToken ct)
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

        await repository.SetUserPermissionAsync(email, permissionName, request.Access, ct);
        logger.LogInformation("Set user {Email} permission {Permission} to {Access}", email, permissionName, request.Access);
        return Ok();
    }

    [HttpDelete("users/{email}/permissions/{permissionName}")]
    public async Task<IActionResult> RemoveUserPermission(string email, string permissionName, CancellationToken ct)
    {
        await repository.RemoveUserPermissionAsync(email, permissionName, ct);
        logger.LogInformation("Removed user {Email} permission {Permission}", email, permissionName);
        return NoContent();
    }

    [HttpDelete("users/{email}")]
    public async Task<IActionResult> DeleteUser(string email, CancellationToken ct)
    {
        logger.LogInformation("Deleting user {Email}", email);
        await repository.DeleteUserAsync(email, ct);
        return NoContent();
    }

    [HttpDelete("groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(string groupId, CancellationToken ct)
    {
        logger.LogInformation("Deleting group {GroupId}", groupId);
        await repository.DeleteGroupAsync(groupId, ct);
        return NoContent();
    }
}
