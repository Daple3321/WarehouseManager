using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;

namespace WarehouseManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
public class ZonesController(IZoneService zoneService) : ControllerBase
{
    [HttpGet("{zoneId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Zone>> GetZoneById(int zoneId)
    {
        if (zoneId < 0) return BadRequest("Id can't be negative");
        
        var zone = await zoneService.GetZone(zoneId);
        if (zone == null) return NotFound($"Item {zoneId} was not found.");
        
        return Ok(zone);
    }
    
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<Zone>> GetZones()
    {
        var zones = await zoneService.GetZones();

        return Ok(zones);
    }
    
    [HttpPost]
    public async Task<ActionResult<Zone>> AddItem([FromBody] ZoneDto zoneDto)
    {
        var zone = await zoneService.AddZone(zoneDto);
        if (zone == null) return BadRequest();
        
        return Created("", zone);
    }
}