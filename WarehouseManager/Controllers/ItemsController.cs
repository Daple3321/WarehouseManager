using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Services;

namespace WarehouseManager.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
public class ItemsController(IItemService itemService) : ControllerBase
{
    [HttpGet("{name}")]
    public async Task<ActionResult<Item>> Get(string name)
    {
        var item = await itemService.GetItemAsync(name);
        
        if (item == null)
        {
            return NotFound($"Item {name} was not found.");
        }
        
        return Ok(item);
    }
}