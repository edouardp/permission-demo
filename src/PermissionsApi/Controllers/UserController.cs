using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/users")]
#pragma warning disable S6960 // Controller responsibilities are focused on user management
public class UserController(
    IPermissionsRepository repository,
    IHistoryService historyService,
    ILogger<UserController> logger)
    : ControllerBase
{
    /// <summary>
    /// Gets calculated permissions for a user, showing final allow/deny status after applying hierarchy
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User's calculated permissions with allow and deny lists</returns>
    /// <response code="200">Permissions calculated successfully</response>
    /// <response code="404">User not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpGet("{email}/permissions")]
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
    /// Creates a new user with optional group memberships
    /// </summary>
    /// <param name="request">User details including email and group assignments</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="201">User created successfully</response>
    /// <response code="400">Invalid request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        if (!EmailValidator.IsValid(request.Email))
        {
            return Problem(
                title: "Invalid Email",
                detail: EmailValidator.ValidationRules,
                statusCode: 400
            );
        }

        logger.LogInformation("Creating user {Email}", request.Email);
        await repository.CreateUserAsync(request.Email, request.Groups, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    /// <summary>
    /// Replaces all permissions for a user with the provided set (batch operation)
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="request">List of permissions with ALLOW/DENY access levels</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">User permissions updated successfully</response>
    /// <response code="400">Invalid permissions or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{email}/permissions")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SetUserPermissions(string email, [FromBody] BatchPermissionRequest request, CancellationToken ct)
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
            logger.LogWarning("Invalid permissions for user {Email}: {InvalidPermissions}", email, string.Join(", ", invalidPermissions));
            return Problem(
                title: "Invalid Permissions",
                detail: $"The following permissions do not exist: {string.Join(", ", invalidPermissions)}",
                statusCode: 400
            );
        }

        // Replace all permissions for this user
        var permissions = new Dictionary<string, string>();
        foreach (var perm in request.Allow)
        {
            permissions[perm] = PermissionAccess.Allow;
        }
        foreach (var perm in request.Deny)
        {
            permissions[perm] = PermissionAccess.Deny;
        }
        
        await repository.SetUserPermissionsAsync(email, permissions, ct, request.Principal, request.Reason);
        logger.LogInformation("Set {PermissionCount} permissions for user {Email}", permissions.Count, email);
        return Ok();
    }

    /// <summary>
    /// Sets a single permission for a user with ALLOW or DENY access
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="permissionName">Permission name</param>
    /// <param name="request">Access level (ALLOW or DENY)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">User permission set successfully</response>
    /// <response code="400">Invalid permission or request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("{email}/permissions/{permissionName}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
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

    /// <summary>
    /// Removes a specific permission from a user
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="permissionName">Permission name to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Permission removed successfully</response>
    [HttpDelete("{email}/permissions/{permissionName}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveUserPermission(string email, string permissionName, CancellationToken ct)
    {
        await repository.RemoveUserPermissionAsync(email, permissionName, ct);
        logger.LogInformation("Removed user {Email} permission {Permission}", email, permissionName);
        return NoContent();
    }

    /// <summary>
    /// Gets change history for a specific user
    /// </summary>
    /// <param name="email">User email address</param>
    /// <returns>List of historical changes for the user</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet("{email}/history")]
    [ProducesResponseType(typeof(List<HistoryEntry>), 200)]
    public async Task<IActionResult> GetUserHistory(string email)
    {
        var history = await historyService.GetEntityHistoryAsync("User", email);
        return Ok(history);
    }

    /// <summary>
    /// Deletes a user and all associated permissions
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">User deleted successfully</response>
    [HttpDelete("{email}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteUser(string email, CancellationToken ct)
    {
        logger.LogInformation("Deleting user {Email}", email);
        await repository.DeleteUserAsync(email, ct);
        return NoContent();
    }
}
