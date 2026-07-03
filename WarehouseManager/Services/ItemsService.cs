using Microsoft.EntityFrameworkCore;
using WarehouseManager.Data;
using WarehouseManager.Models;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IItemService
{
    Task<Item?> GetItemAsync(string name);
    Task<List<Item>> GetItemsAsync(int page, int pageSize);
    Task AddItemsAsync(IReadOnlyCollection<Item> items);
    Task<Item> MoveItemAsync(int itemId, int zoneId);
}

public class ItemsService(AppDbContext dbContext, ILogger<ItemsService> logger) : IItemService
{
    public async Task<Item?> GetItemAsync(string name)
    {
        logger.LogDebug("Get Item: {name}", name);
        
        await Task.Delay(TimeSpan.FromSeconds(2));

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return new Item(name, "Some desc...", ItemState.InTransit, 125, 1);
    }


    public async Task<List<Item>> GetItemsAsync(int page, int pageSize)
    {
        return await dbContext.Items.AsNoTracking().ToListAsync();
    }
    
    public async Task AddItemsAsync(IReadOnlyCollection<Item> items)
    {
    
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