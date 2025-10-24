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

    [HttpGet("permissions/user/{email}")]
    public async Task<IActionResult> GetPermissions(string email, CancellationToken ct)
    {
        var permissions = await _repository.CalculatePermissionsAsync(email, ct);
        if (permissions == null)
        {
            _logger.LogWarning("User {Email} not found", email);
            return NotFound();
        }
        return Ok(new PermissionsResponse { Email = email, Permissions = permissions });
    }

    [HttpPost("groups")]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating group {GroupName}", request.Name);
        var group = await _repository.CreateGroupAsync(request.Name, ct);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    [HttpPost("groups/{groupId}/permissions")]
    public async Task<IActionResult> SetGroupPermission(string groupId, [FromBody] PermissionRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Setting group {GroupId} permission {Permission} to {Access}", groupId, request.Permission, request.Access);
        await _repository.SetGroupPermissionAsync(groupId, request.Permission, request.Access, ct);
        return Ok();
    }

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Creating user {Email}", request.Email);
        await _repository.CreateUserAsync(request.Email, request.Groups, ct);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    [HttpPost("users/{email}/permissions")]
    public async Task<IActionResult> SetUserPermission(string email, [FromBody] PermissionRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Setting user {Email} permission {Permission} to {Access}", email, request.Permission, request.Access);
        await _repository.SetUserPermissionAsync(email, request.Permission, request.Access, ct);
        return Ok();
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
