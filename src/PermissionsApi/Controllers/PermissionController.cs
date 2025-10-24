using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1")]
#pragma warning disable S6960 // Controller responsibilities are appropriate for this API size
public class PermissionController(
    IPermissionsRepository repository,
    IHistoryService historyService,
    ILogger<PermissionController> logger)
    : ControllerBase
{
    /// <summary>
    /// Creates a new permission with optional default status
    /// </summary>
    /// <param name="request">Permission details including name, description, and default status</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The created permission</returns>
    /// <response code="201">Permission created successfully</response>
    /// <response code="400">Invalid request data. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPost("permissions")]
    [ProducesResponseType(typeof(Permission), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken ct)
    {
        if (!PermissionNameValidator.IsValid(request.Name))
        {
            return Problem(
                title: "Invalid Permission Name",
                detail: PermissionNameValidator.ValidationRules,
                statusCode: 400
            );
        }

        logger.LogInformation("Creating permission {PermissionName} (IsDefault: {IsDefault})", request.Name, request.IsDefault);
        var permission = await repository.CreatePermissionAsync(request.Name, request.Description, request.IsDefault, ct, request.Principal, request.Reason);
        return CreatedAtAction(nameof(GetPermission), new { name = permission.Name }, permission);
    }

    /// <summary>
    /// Gets all permissions in the system
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of all permissions</returns>
    /// <response code="200">Permissions retrieved successfully</response>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(List<Permission>), 200)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var permissions = await repository.GetAllPermissionsAsync(ct);
        return Ok(permissions);
    }

    /// <summary>
    /// Gets a specific permission by name
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The requested permission</returns>
    /// <response code="200">Permission found</response>
    /// <response code="404">Permission not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpGet("permissions/{name}")]
    [ProducesResponseType(typeof(Permission), 200)]
    [ProducesResponseType(404)]
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

    /// <summary>
    /// Updates a permission's description
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <param name="request">Updated permission details</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Permission updated successfully</response>
    /// <response code="404">Permission not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("permissions/{name}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
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

    /// <summary>
    /// Deletes a permission from the system
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="204">Permission deleted successfully</response>
    /// <response code="404">Permission not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpDelete("permissions/{name}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
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

    /// <summary>
    /// Sets or unsets a permission as a system default
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <param name="isDefault">Whether this permission should be a system default</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Default status updated successfully</response>
    /// <response code="404">Permission not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpPut("permissions/{name}/default")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
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
    /// Gets change history for a specific permission
    /// </summary>
    /// <param name="name">Permission name</param>
    /// <returns>List of historical changes for the permission</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet("permissions/{name}/history")]
    [ProducesResponseType(typeof(List<HistoryEntry>), 200)]
    public async Task<IActionResult> GetPermissionHistory(string name)
    {
        var history = await historyService.GetEntityHistoryAsync("Permission", name);
        return Ok(history);
    }
}
