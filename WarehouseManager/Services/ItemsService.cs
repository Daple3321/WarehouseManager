using Microsoft.EntityFrameworkCore;
using WarehouseManager.Data;
using WarehouseManager.Models;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IItemService
{
    Task<Item?> GetItemAsync(int itemId);
    Task<PagedResult<Item>> GetItemsPaginatedAsync(int page, int pageSize);
    Task<Item> AddItemAsync(ItemDto item);
    Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items);
    Task<Item> MoveItemAsync(int itemId, int zoneId);
}

public class ItemsService(AppDbContext dbContext, ILogger<ItemsService> logger) : IItemService
{
    public async Task<Item?> GetItemAsync(int itemId)
    {
        //logger.LogDebug("Get Item: {name}", name);

        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
                   ?? throw new KeyNotFoundException($"Item {itemId} not found");

        return item;
    }

    public async Task<PagedResult<Item>> GetItemsPaginatedAsync(int page, int pageSize)
    {
        int itemsToSkip = (page - 1) * pageSize;
        int totalItems = await dbContext.Items.CountAsync();
        
        var items = await dbContext.Items
            .Skip(itemsToSkip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return new PagedResult<Item>(items, totalItems, page, pageSize);
    }
    
    public async Task<Item> AddItemAsync(ItemDto item)
    {
        var newItem = new Item(item.ItemName, item.Description, ItemState.Inspection, 0, item.ZoneId);
        dbContext.Items.Add(newItem);
        await dbContext.SaveChangesAsync();

        return newItem;
    }
    
    public async Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items)
    {
        var newItems = items.Select(dto => new Item(
            dto.ItemName, 
            dto.Description, 
            ItemState.Inspection,
            0, 
            dto.ZoneId));
        
        dbContext.Items.AddRange(newItems);
        await dbContext.SaveChangesAsync();
        
        // BUG: Not returning actual created db entities. Id is not populated
        return newItems;
    }

    public async Task<Item> MoveItemAsync(int itemId, int zoneId)
    {
        var item = await dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found");

        if (item.State == ItemState.Defected)
            throw new InvalidOperationException("Cannot move a defected item");

        dbContext.Entry(item).Property(i => i.ZoneId).CurrentValue = zoneId;
        await dbContext.SaveChangesAsync();
        return item with { ZoneId = zoneId };
    }
}