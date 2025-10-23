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

    [HttpGet("permissions/{email}")]
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
