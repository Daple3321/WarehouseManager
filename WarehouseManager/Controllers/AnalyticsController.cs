using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace WarehouseManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
public class AnalyticsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDefects()
    {
        return Ok();
    }
}