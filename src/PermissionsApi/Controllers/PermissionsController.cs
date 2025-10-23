using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api")]
public class PermissionsController : ControllerBase
{
    private readonly PermissionsRepository _repository;

    public PermissionsController(PermissionsRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("permissions")]
    public IActionResult CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var permission = _repository.CreatePermission(request.Name, request.Description);
        return CreatedAtAction(nameof(GetPermission), new { name = permission.Name }, permission);
    }

    [HttpGet("permissions")]
    public IActionResult GetAllPermissions()
    {
        var permissions = _repository.GetAllPermissions();
        return Ok(permissions);
    }

    [HttpGet("permissions/{name}")]
    public IActionResult GetPermission(string name)
    {
        var permission = _repository.GetPermission(name);
        if (permission == null)
            return NotFound();
        return Ok(permission);
    }

    [HttpPut("permissions/{name}")]
    public IActionResult UpdatePermission(string name, [FromBody] UpdatePermissionRequest request)
    {
        if (!_repository.UpdatePermission(name, request.Description))
            return NotFound();
        return Ok();
    }

    [HttpDelete("permissions/{name}")]
    public IActionResult DeletePermission(string name)
    {
        if (!_repository.DeletePermission(name))
            return NotFound();
        return NoContent();
    }

    [HttpGet("permissions/user/{email}")]
    public IActionResult GetPermissions(string email)
    {
        var permissions = _repository.CalculatePermissions(email);
        return Ok(new PermissionsResponse { Email = email, Permissions = permissions });
    }

    [HttpPost("groups")]
    public IActionResult CreateGroup([FromBody] CreateGroupRequest request)
    {
        var group = _repository.CreateGroup(request.Name);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    [HttpPost("groups/{groupId}/permissions")]
    public IActionResult SetGroupPermission(string groupId, [FromBody] PermissionRequest request)
    {
        _repository.SetGroupPermission(groupId, request.Permission, request.Access);
        return Ok();
    }

    [HttpPost("users")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        _repository.CreateUser(request.Email, request.Groups);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    [HttpPost("users/{email}/permissions")]
    public IActionResult SetUserPermission(string email, [FromBody] PermissionRequest request)
    {
        _repository.SetUserPermission(email, request.Permission, request.Access);
        return Ok();
    }

    [HttpDelete("users/{email}")]
    public IActionResult DeleteUser(string email)
    {
        _repository.DeleteUser(email);
        return NoContent();
    }

    [HttpDelete("groups/{groupId}")]
    public IActionResult DeleteGroup(string groupId)
    {
        _repository.DeleteGroup(groupId);
        return NoContent();
    }
}
