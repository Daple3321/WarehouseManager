using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;

namespace WarehouseManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
public class AnalyticsController(IDefectService defectService, IItemService itemsService) : ControllerBase
{
    /// <summary>
    /// Get defects analytics for specified amount of days
    /// </summary>
    /// <returns></returns>
    [HttpGet("defects/{days:int}")]
    public async Task<ActionResult<DefectsAnalytics>> GetDefects(int days)
    {
        var analytics = await defectService.GetAnalytics(days);
        
        return Ok(analytics);
    }
    
    [HttpGet("deliveries/{days:int}")]
    public async Task<ActionResult<DeliveriesAnalytics>> GetDeliveries(int days)
    {
        var analytics = await itemsService.GetAnalytics(days);
        
        return Ok(analytics);
    }
}