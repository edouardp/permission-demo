using Microsoft.AspNetCore.Mvc;
using PermissionsApi.Services;

namespace PermissionsApi.Controllers;

[ApiController]
[Route("api/v1/history")]
public class HistoryController : ControllerBase
{
    private readonly IHistoryService _historyService;

    public HistoryController(IHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory([FromQuery] int? skip = null, [FromQuery] int? count = null)
    {
        var history = await _historyService.GetHistoryAsync(skip, count);
        return Ok(history);
    }
}
