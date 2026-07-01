namespace WarehouseManager.Services;

public interface IItemService
{
    Task<Item?> GetItemAsync(string name);
}

public record Item(string ItemName, string Description);

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

        return new Item(name, "Some desc...");
    }
}