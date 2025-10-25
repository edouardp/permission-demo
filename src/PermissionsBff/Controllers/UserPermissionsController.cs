using Microsoft.AspNetCore.Mvc;

namespace PermissionsBff.Controllers;

[ApiController]
[Route("bff/permissions")]
public class UserPermissionsController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("user/{email}")]
    public async Task<IActionResult> GetUserPermissions(string email)
    {
        var client = httpClientFactory.CreateClient("PermissionsApi");
        var response = await client.GetAsync($"/api/v1/users/{email}/permissions");

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        var permissions = await response.Content.ReadFromJsonAsync<PermissionsResponse>();
        return Ok(new { allow = permissions?.Allow ?? [] });
    }
}

public record PermissionsResponse(string Email, List<string> Allow, List<string> Deny);
