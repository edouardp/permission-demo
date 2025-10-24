using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/history")]
public class HistoryController(IHistoryService historyService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int? skip = null, [FromQuery] int? count = null)
    {
        var history = await historyService.GetHistoryAsync(skip, count);
        return Ok(history);
    }
}
