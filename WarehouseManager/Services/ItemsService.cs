using WarehouseManager.Models;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IItemService
{
    Task<Item?> GetItemAsync(string name);
}

public class ItemsService(ILogger<ItemsService> logger) : IItemService
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


    public async Task GetItemsAsync(int page, int pageSize)
    {
        
    }
    
    public async Task AddItemsAsync(IReadOnlyCollection<Item> items)
    {
    
    }

    public async Task MoveItemAsync(int itemId)
    {
        
    }
}