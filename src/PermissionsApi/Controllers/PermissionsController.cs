using Microsoft.AspNetCore.Mvc;

namespace PermissionsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPermissions()
        {
            return Ok(new { message = "Permissions API ready" });
        }
    }
}
