using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;

namespace WarehouseManager.Controllers;

// TODO: Things to clear-up:
// 1) Why is there two places for error handling?? IN service (throws) and in this controller. Which one actually returns?
// 2) Error handling is bad. In image uploads and other places the stack traces leak.
[ApiController]
[ApiVersion("1.0")]
[Route("[controller]")]
public class ItemsController(IItemService itemService, IZoneService zoneService, IDefectService defectService) : ControllerBase
{
    [HttpGet("{itemId:int}")]
    [EndpointSummary("Retrieves an item by its id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Item>> GetItemById(int itemId)
    {
        if (itemId < 0) return BadRequest("Id can't be negative");
        
        var item = await itemService.GetItemAsync(itemId);
        
        if (item == null) return NotFound($"Item {itemId} was not found.");
        
        return Ok(item);
    }
    
    [HttpGet("{itemId:int}/image")]
    [EndpointSummary("Retrieves an item image by item id")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Item>> GetImageForItem(int itemId)
    {
        if (itemId < 0) return BadRequest("Id can't be negative");
        
        var image = await itemService.GetImageForItem(itemId);
        if (image == null) return NotFound($"Image for item {itemId} was not found.");
        
        // BUG: Content type here is not always jpg
        return File(image, "image/jpg");
    }
    
    [HttpGet]
    public async Task<ActionResult<PagedResult<Item>>> GetItemsPaginated()
    {
        int page = 1;
        int pageSize = 20;
        int categoryId = -1;

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
        
        if (HttpContext.Request.Query.TryGetValue("categoryId", out var categoryVals))
        {
            string firstValue = categoryVals.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(firstValue)) return BadRequest("No categoryId value defined.");

            if (!int.TryParse(firstValue, out int result)) return BadRequest("No categoryId parameter found.");
            
            categoryId = result;
        }

        var items = await itemService.GetItemsPaginatedAsync(page, pageSize, categoryId);
 
        return Ok(items);
    }
    
    [HttpGet("zone/{zoneId:int}")]
    public async Task<ActionResult<PagedResult<Item>>> GetItemsInZone(int zoneId)
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

        var items = await zoneService.GetItemsInZone(zoneId, page, pageSize);
       
        return Ok(items);
    }
    
    /// <summary>
    /// Accept a batch of items and instantiate them with 'Received' state.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveItems([FromBody] List<ItemDto> items)
    {
        if (items == null || !items.Any())
        {
            return BadRequest("Invalid request body. No items to add.");
        }

        var created = await itemService.AddItemsAsync(items);
        
        return Created("", created);
    }
    
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<Item>> AddItem([FromForm] ItemDto item)
    {
        if (string.IsNullOrEmpty(item.ItemName))
        {
            return BadRequest("Invalid request body. No item name specified.");
        }
        
        var createdItem = await itemService.AddItemAsync(item);
        if (createdItem == null)
            return BadRequest();

        //return CreatedAtRoute(nameof(GetItemById), new { Version = "1", id = createdItem.Id }, createdItem);
        return Created("", createdItem);
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> DeleteItem(int itemId)
    {
        if (itemId < 0) return BadRequest("ItemId can't be negative");

        await itemService.DeleteItem(itemId);
        
        return Ok("Item deleted successfully");
    }

    /// <summary>
    /// Move an item to another storage zone.
    /// </summary>
    /// <returns></returns>
    [HttpPut("move")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Item>> MoveItem([FromBody] MoveDto moveDto)
    {
        var itemsInZone = await zoneService.GetItemCountInZone(moveDto.ZoneId);
        if (itemsInZone.count >= itemsInZone.zone.MaxItems)
            return Conflict($"Can't move item: {moveDto.ItemId} to zone: {moveDto.ZoneId}. It is already full.");
        
        var item = await itemService.MoveItemAsync(moveDto.ItemId, moveDto.ZoneId);
        return Ok(item);
    }
    
    [HttpPut("{itemId:int}/state/{state}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Item>> ChangeItemState(int itemId, ItemState state)
    {
        var item = await itemService.ChangeItemState(itemId, state);
        
        return Ok(item);
    }
    
    [HttpPost("{itemId:int}/defect")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DefectReport>> DefectItem(int itemId, [FromForm] DefectReportDto defectReportDto)
    {
        if (defectReportDto.DefectImage == null || defectReportDto.DefectImage.Length == 0)
            return BadRequest("File is empty.");

        var report = await defectService.CreateReport(defectReportDto);
        
        return Created($"{itemId}/defect", report);
    }

    [HttpGet("{reportId:int}/defect")]
    public async Task<ActionResult<DefectReport>> GetDefectReport(int reportId)
    {
        var report = await defectService.GetReport(reportId);

        return report;
    }
    
    [HttpGet("{reportId:int}/defect/image")]
    [EndpointSummary("Retrieves report image by reportId")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Item>> GetImageForReport(int reportId)
    {
        if (reportId < 0) return BadRequest("Id can't be negative");
        
        var image = await defectService.GetImageForReport(reportId);
        if (image == null) return NotFound($"Image for report {reportId} was not found.");
        
        // BUG: Content type here is not always jpg
        return File(image, "image/jpg");
    }
    
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<Category>>> GetCategories()
    {
        var categories = await itemService.GetCategories();
        
        return Ok(categories);
    }
}