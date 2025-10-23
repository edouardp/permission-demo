using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api")]
public class PermissionsController : ControllerBase
{
    private readonly DataStore _dataStore;

    public PermissionsController(DataStore dataStore)
    {
        _dataStore = dataStore;
    }

    [HttpGet("permissions/{email}")]
    public IActionResult GetPermissions(string email)
    {
        var permissions = _dataStore.CalculatePermissions(email);
        return Ok(new PermissionsResponse { Email = email, Permissions = permissions });
    }

    [HttpPost("groups")]
    public IActionResult CreateGroup([FromBody] CreateGroupRequest request)
    {
        var group = _dataStore.CreateGroup(request.Name);
        return CreatedAtAction(nameof(CreateGroup), new { id = group.Id, name = group.Name });
    }

    [HttpPost("groups/{groupId}/permissions")]
    public IActionResult SetGroupPermission(string groupId, [FromBody] PermissionRequest request)
    {
        _dataStore.SetGroupPermission(groupId, request.Permission, request.Access);
        return Ok();
    }

    [HttpPost("users")]
    public IActionResult CreateUser([FromBody] CreateUserRequest request)
    {
        _dataStore.CreateUser(request.Email, request.Groups);
        return CreatedAtAction(nameof(CreateUser), null);
    }

    [HttpPost("users/{email}/permissions")]
    public IActionResult SetUserPermission(string email, [FromBody] PermissionRequest request)
    {
        _dataStore.SetUserPermission(email, request.Permission, request.Access);
        return Ok();
    }

    [HttpDelete("users/{email}")]
    public IActionResult DeleteUser(string email)
    {
        _dataStore.DeleteUser(email);
        return NoContent();
    }

    [HttpDelete("groups/{groupId}")]
    public IActionResult DeleteGroup(string groupId)
    {
        _dataStore.DeleteGroup(groupId);
        return NoContent();
    }
}
