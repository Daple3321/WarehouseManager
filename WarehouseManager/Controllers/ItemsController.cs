using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;
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
    
    [HttpGet]
    public async Task<ActionResult<List<Item>>> GetItemsPaginated()
    {
        int page = 1;
        int pageSize = 20;

        if (HttpContext.Request.Query.TryGetValue("page", out var pageVals))
        {
            string firstValue = pageVals.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstValue)) return BadRequest("No page value defined.");

            if (!int.TryParse(firstValue, out int result)) return BadRequest("No page parameter found.");
            
            page = result;
        }
        
        if (HttpContext.Request.Query.TryGetValue("pageSize", out var sizeVals))
        {
            string firstValue = sizeVals.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstValue)) return BadRequest("No pageSize value defined.");

            if (!int.TryParse(firstValue, out int result)) return BadRequest("No pageSize parameter found.");
            
            pageSize = result;
        }
        
        
        return Ok($"Page = {page}, PageSize = {pageSize}");
    }
    
    /// <summary>
    /// Accept a batch of items and instantiate them with 'Inspection' state.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> ReceiveItems([FromBody] List<ItemDto> items)
    {
        if (items == null || !items.Any())
        {
            return BadRequest("Invalid request body. No items to add.");
        }
        
        return Created();
    }
    
    /// <summary>
    /// Move an item to another storage zone.
    /// </summary>
    /// <returns></returns>
    [HttpPut("move")]
    public async Task<IActionResult> MoveItem([FromBody] MoveDto moveDto)
    {
        
        return Ok();
    }
    
    [HttpPost("{itemId:int}/defect")]
    public async Task<IActionResult> DefectItem(int itemId, [FromForm] DefectDto defectDto)
    {
        if (defectDto.DefectImage == null || defectDto.DefectImage.Length == 0)
            return BadRequest("File is empty.");

        // Example: Reading file data into a byte array for database storage
        using var memoryStream = new MemoryStream();
        await defectDto.DefectImage.CopyToAsync(memoryStream);
        byte[] fileBytes = memoryStream.ToArray();
        
        return Ok(new { Size = fileBytes.Length, Name = defectDto.DefectImage.FileName });
    }
}