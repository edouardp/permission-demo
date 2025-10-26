using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/user")]
public class DebugController(IPermissionsRepository repository, ILogger<DebugController> logger) : ControllerBase
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
        try
        {
            logger.LogDebug("Getting permission debug info for user {Email}", email);
            var debug = await repository.CalculatePermissionsDebugAsync(email, ct);
            if (debug == null)
            {
                logger.LogWarning("User {Email} not found for debug request", email);
                return NotFound();
            }

            logger.LogDebug("Successfully retrieved debug info for user {Email} with {PermissionCount} permissions", 
                email, debug.Permissions.Count);
            return Ok(debug);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get debug info for user {Email}", email);
            throw new OperationException("Operation failed", ex);
        }
    }
}
