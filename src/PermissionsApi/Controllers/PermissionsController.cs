using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionsRepository _repository;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IPermissionsRepository repository, ILogger<PermissionsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("permissions")]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating permission {PermissionName} (IsDefault: {IsDefault})", request.Name, request.IsDefault);
        var permission = await _repository.CreatePermissionAsync(request.Name, request.Description, request.IsDefault, ct);
        return CreatedAtAction(nameof(GetPermission), new { name = permission.Name }, permission);
    }

    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions(CancellationToken ct)
    {
        var permissions = await _repository.GetAllPermissionsAsync(ct);
        return Ok(permissions);
    }

    [HttpGet("permissions/{name}")]
    public async Task<IActionResult> GetPermission(string name, CancellationToken ct)
    {
        var permission = await _repository.GetPermissionAsync(name, ct);
        if (permission == null)
        {
            _logger.LogWarning("Permission {PermissionName} not found", name);
            return NotFound();
        }
        return Ok(permission);
    }

    [HttpPut("permissions/{name}")]
    public async Task<IActionResult> UpdatePermission(string name, [FromBody] UpdatePermissionRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Updating permission {PermissionName}", name);
        if (!await _repository.UpdatePermissionAsync(name, request.Description, ct))
        {
            _logger.LogWarning("Permission {PermissionName} not found for update", name);
            return NotFound();
        }
        return Ok();
    }

    [HttpDelete("permissions/{name}")]
    public async Task<IActionResult> DeletePermission(string name, CancellationToken ct)
    {
        _logger.LogInformation("Deleting permission {PermissionName}", name);
        if (!await _repository.DeletePermissionAsync(name, ct))
        {
            _logger.LogWarning("Permission {PermissionName} not found for deletion", name);
            return NotFound();
        }
        return NoContent();
    }

    [HttpPut("permissions/{name}/default")]
    public async Task<IActionResult> SetPermissionDefault(string name, [FromBody] bool isDefault, CancellationToken ct)
    {
        _logger.LogInformation("Setting permission {PermissionName} IsDefault to {IsDefault}", name, isDefault);
        if (!await _repository.SetPermissionDefaultAsync(name, isDefault, ct))
        {
            _logger.LogWarning("Permission {PermissionName} not found", name);
            return NotFound();
        }
        return Ok();
    }

    [HttpGet("users/{email}/permissions")]
    public async Task<IActionResult> GetPermissions(string email, CancellationToken ct)
    {
        var permissions = await _repository.CalculatePermissionsAsync(email, ct);
        if (permissions == null)
        {
            _logger.LogWarning("User {Email} not found", email);
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

    [HttpGet("user/{email}/debug")]
    public async Task<IActionResult> GetUserPermissionsDebug(string email, CancellationToken ct)
    {
        var debug = await _repository.CalculatePermissionsDebugAsync(email, ct);
        if (debug == null)
        {
            return NotFound();
        }

        return Ok(debug);
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating group {GroupName}", request.Name);
        var group = await _repository.CreateGroupAsync(request.Name, ct);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    [HttpPut("groups/{groupId}/permissions")]
    public async Task<IActionResult> SetGroupPermissions(string groupId, [FromBody] BatchPermissionRequest request, CancellationToken ct)
    {
        // Validate all permissions exist
        var invalidPermissions = new List<string>();
        foreach (var permissionRequest in request.Permissions)
        {
            var permission = await _repository.GetPermissionAsync(permissionRequest.Permission, ct);
            if (permission == null)
            {
                invalidPermissions.Add(permissionRequest.Permission);
            }
        }

        if (invalidPermissions.Count > 0)
        {
            _logger.LogWarning("Invalid permissions for group {GroupId}: {InvalidPermissions}", groupId, string.Join(", ", invalidPermissions));
            return Problem(
                title: "Invalid Permissions",
                detail: $"The following permissions do not exist: {string.Join(", ", invalidPermissions)}",
                statusCode: 400
            );
        }

        // Replace all permissions for this group
        await _repository.ReplaceGroupPermissionsAsync(groupId, request.Permissions, ct);

        _logger.LogInformation("Replaced permissions for group {GroupId} with {Count} permissions", groupId, request.Permissions.Count);
        return Ok();
    }

    [HttpPut("groups/{groupId}/permissions/{permissionName}")]
    public async Task<IActionResult> SetGroupPermission(string groupId, string permissionName, [FromBody] PermissionAccessRequest request, CancellationToken ct)
    {
        // Validate permission exists
        var permission = await _repository.GetPermissionAsync(permissionName, ct);
        if (permission == null)
        {
            _logger.LogWarning("Permission {PermissionName} not found", permissionName);
            return Problem(
                title: "Invalid Permission",
                detail: $"Permission '{permissionName}' does not exist",
                statusCode: 400
            );
        }

        await _repository.SetGroupPermissionAsync(groupId, permissionName, request.Access, ct);
        _logger.LogInformation("Set group {GroupId} permission {Permission} to {Access}", groupId, permissionName, request.Access);
        return Ok();
    }

    [HttpDelete("groups/{groupId}/permissions/{permissionName}")]
    public async Task<IActionResult> RemoveGroupPermission(string groupId, string permissionName, CancellationToken ct)
    {
        await _repository.RemoveGroupPermissionAsync(groupId, permissionName, ct);
        _logger.LogInformation("Removed group {GroupId} permission {Permission}", groupId, permissionName);
        return NoContent();
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating user {Email}", request.Email);
        await _repository.CreateUserAsync(request.Email, request.Groups, ct);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    [HttpPut("users/{email}/permissions")]
    public async Task<IActionResult> SetUserPermissions(string email, [FromBody] BatchPermissionRequest request, CancellationToken ct)
    {
        // Validate all permissions exist
        var invalidPermissions = new List<string>();
        foreach (var permissionRequest in request.Permissions)
        {
            var permission = await _repository.GetPermissionAsync(permissionRequest.Permission, ct);
            if (permission == null)
            {
                invalidPermissions.Add(permissionRequest.Permission);
            }
        }

        if (invalidPermissions.Count > 0)
        {
            _logger.LogWarning("Invalid permissions for user {Email}: {InvalidPermissions}", email, string.Join(", ", invalidPermissions));
            return Problem(
                title: "Invalid Permissions",
                detail: $"The following permissions do not exist: {string.Join(", ", invalidPermissions)}",
                statusCode: 400
            );
        }

        // Replace all permissions for this user
        await _repository.ReplaceUserPermissionsAsync(email, request.Permissions, ct);

        _logger.LogInformation("Replaced permissions for user {Email} with {Count} permissions", email, request.Permissions.Count);
        return Ok();
    }

    [HttpPut("users/{email}/permissions/{permissionName}")]
    public async Task<IActionResult> SetUserPermission(string email, string permissionName, [FromBody] PermissionAccessRequest request, CancellationToken ct)
    {
        // Validate permission exists
        var permission = await _repository.GetPermissionAsync(permissionName, ct);
        if (permission == null)
        {
            _logger.LogWarning("Permission {PermissionName} not found", permissionName);
            return Problem(
                title: "Invalid Permission",
                detail: $"Permission '{permissionName}' does not exist",
                statusCode: 400
            );
        }

        await _repository.SetUserPermissionAsync(email, permissionName, request.Access, ct);
        _logger.LogInformation("Set user {Email} permission {Permission} to {Access}", email, permissionName, request.Access);
        return Ok();
    }

    [HttpDelete("users/{email}/permissions/{permissionName}")]
    public async Task<IActionResult> RemoveUserPermission(string email, string permissionName, CancellationToken ct)
    {
        await _repository.RemoveUserPermissionAsync(email, permissionName, ct);
        _logger.LogInformation("Removed user {Email} permission {Permission}", email, permissionName);
        return NoContent();
    }

    [HttpDelete("users/{email}")]
    public async Task<IActionResult> DeleteUser(string email, CancellationToken ct)
    {
        _logger.LogInformation("Deleting user {Email}", email);
        await _repository.DeleteUserAsync(email, ct);
        return NoContent();
    }

    [HttpDelete("groups/{groupId}")]
    public async Task<IActionResult> DeleteGroup(string groupId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting group {GroupId}", groupId);
        await _repository.DeleteGroupAsync(groupId, ct);
        return NoContent();
    }
}
