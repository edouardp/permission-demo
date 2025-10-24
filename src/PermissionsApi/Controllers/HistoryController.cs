using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Models;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/history")]
public class HistoryController(IHistoryService historyService) : ControllerBase
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
        var history = await historyService.GetHistoryAsync(skip, count);
        return Ok(history);
    }
}
