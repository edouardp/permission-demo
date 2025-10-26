using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Exceptions;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/history")]
public class HistoryController(IHistoryService historyService, ILogger<HistoryController> logger) : ControllerBase
{
    /// <summary>
    /// Gets global change history for all entities with optional pagination
    /// </summary>
    /// <param name="skip">Number of records to skip for pagination</param>
    /// <param name="count">Maximum number of records to return</param>
    /// <returns>List of historical changes across all entities</returns>
    /// <response code="200">History retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<HistoryEntry>), 200)]
    public async Task<IActionResult> GetHistory([FromQuery] int? skip = null, [FromQuery] int? count = null)
    {
        try
        {
            logger.LogDebug("Getting history with skip={Skip}, count={Count}", skip, count);
            var history = await historyService.GetHistoryAsync(skip, count);
            logger.LogDebug("Retrieved {HistoryCount} history entries", history.Count);
            return Ok(history);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get history with skip={Skip}, count={Count}", skip, count);
            throw new OperationException("Operation failed", ex);
        }
    }
}
