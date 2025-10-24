using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/user")]
public class DebugController(IPermissionsRepository repository) : ControllerBase
{
    /// <summary>
    /// Debug permission resolution chain showing how each permission is calculated through Default → Group → User hierarchy
    /// </summary>
    /// <param name="email">User email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed permission resolution chain for debugging</returns>
    /// <response code="200">Debug information retrieved successfully</response>
    /// <response code="404">User not found. Response is RFC 9457 Problem Details JSON.</response>
    [HttpGet("{email}/debug")]
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
}
