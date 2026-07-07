using Microsoft.EntityFrameworkCore;
using WarehouseManager.Data;
using WarehouseManager.Models;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IItemService
{
    Task<Item?> GetItemAsync(int itemId);
    Task<PagedResult<Item>> GetItemsPaginatedAsync(int page, int pageSize, int categoryId = -1);
    Task<FileStream?> GetImageForItem(int itemId);
    
    Task<Item> AddItemAsync(ItemDto item);
    Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items);
    
    Task<Item> MoveItemAsync(int itemId, int zoneId);
    Task<Item> ChangeItemState(int itemId, ItemState newState);
    
    Task DeleteItem(int itemId);
    Task<DeliveriesAnalytics> GetAnalytics(int days);
}

public class ItemsService : IItemService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ItemsService> _logger;
    
    private static readonly string StorageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _storagePath = Path.Combine(StorageFolderPath, "ItemImages");
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png"};
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public ItemsService(AppDbContext dbContext, ILogger<ItemsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        
        //_logger.LogDebug("LocalApplicationData path: {path}", StorageFolderPath);
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }
    
    public async Task<Item?> GetItemAsync(int itemId)
    {
        var item = await _dbContext.Items
                       .Include(x => x.Zone)
                       .Include(x => x.Category)
                       .FirstOrDefaultAsync(i => i.Id == itemId)
                   ?? throw new KeyNotFoundException($"Item {itemId} not found");

        return item;
    }
    
    public async Task<FileStream?> GetImageForItem(int itemId)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
                   ?? throw new KeyNotFoundException($"Item {itemId} not found");
        
        if(string.IsNullOrEmpty(item.ImageName)) return null;
        
        var fullPath = Path.Combine(_storagePath, item.ImageName);
        var fileStream = File.OpenRead(fullPath);

        return fileStream;
    }


    public async Task<PagedResult<Item>> GetItemsPaginatedAsync(int page, int pageSize, int categoryId = -1)
    {
        int itemsToSkip = (page - 1) * pageSize;
        int totalItems = await _dbContext.Items.CountAsync();

        List<Item> items;
        if (categoryId > 0)
        {
            var category = await _dbContext.Categories.FirstOrDefaultAsync(x => x.Id == categoryId)
                           ?? throw new KeyNotFoundException($"Category {categoryId} not found");
            
            items = await _dbContext.Items
                .Where(x => x.CategoryId == categoryId)
                .Skip(itemsToSkip)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }
        else
        {
            items = await _dbContext.Items
                .Skip(itemsToSkip)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();
        }
        
        // var items = await _dbContext.Items
        //     .Skip(itemsToSkip)
        //     .Take(pageSize)
        //     .AsNoTracking()
        //     .ToListAsync();

        return new PagedResult<Item>(items, totalItems, page, pageSize);
    }
    
    public async Task<Item> AddItemAsync(ItemDto item)
    {
        var category = await _dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.CategoryId)
                       ?? throw new KeyNotFoundException($"Category {item.CategoryId} not found");
        
        var imageName = await TryUploadItemImage(item.ImageFile); 
        
        var newItem = new Item(item.ItemName, item.Description, ItemState.Received, 0, item.ZoneId, item.CategoryId, DateTime.UtcNow, imageName);
        _dbContext.Items.Add(newItem);
        await _dbContext.SaveChangesAsync();

        return newItem;
    }
    
    public async Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items)
    {
        var newItems = new List<Item>(items.Count);
        
        foreach (var item in items)
        {
            var category = await _dbContext.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.CategoryId)
                           ?? throw new KeyNotFoundException($"Category {item.CategoryId} not found");
            
            var imagePath = await TryUploadItemImage(item.ImageFile);
            
            newItems.Add(new Item(
                item.ItemName,
                item.Description, 
                ItemState.Received, 
                0, 
                item.ZoneId,
                item.CategoryId,
                DateTime.UtcNow,
                imagePath)
            );
        }
        
        _dbContext.Items.AddRange(newItems);
        await _dbContext.SaveChangesAsync();
        
        return newItems;
    }

    private async Task<string?> TryUploadItemImage(IFormFile imageFile)
    {
        // Check if a file was selected
        if (imageFile == null || imageFile.Length == 0) return null;
            //throw new InvalidOperationException("No file uploaded.");
        // Validate file size
        if (imageFile.Length > MaxFileSize)
            throw new InvalidOperationException("File size exceeds the 5 MB limit.");
        // Validate file extension
        var extension = Path.GetExtension(imageFile.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type.");
        
        // Generate unique filename
        var trustedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_storagePath, trustedFileName);

        try
        {
            // Stream and save the file asynchronously
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when uploading item image");
            throw;
        }

        return trustedFileName;
    }

    private void TryDeleteItemImage(string imageName)
    {
        if(string.IsNullOrEmpty(imageName)) return;
        
        var fullPath = Path.Combine(_storagePath, imageName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Image {name} successfully deleted.", imageName);
            }
            else
            {
                _logger.LogWarning("File not found.");
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("Error: You do not have permission to delete this file or it is read-only.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error: The file is locked by another process. Details: {msg}", ex.Message);
        }
    }

    public async Task<Item> MoveItemAsync(int itemId, int zoneId)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found");
        
        var zone = await _dbContext.Zones.AsNoTracking().FirstOrDefaultAsync(i => i.Id == zoneId)
                   ?? throw new KeyNotFoundException($"Zone {itemId} not found");
        

        if (item.ZoneId == zoneId)
            throw new InvalidOperationException($"Cannot move item to zone: {zoneId}. It is already in it.");

        _dbContext.Entry(item).Property(i => i.ZoneId).CurrentValue = zoneId;
        await _dbContext.SaveChangesAsync();
        return item;
    }
    
    public async Task<Item> ChangeItemState(int itemId, ItemState newState)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
                   ?? throw new KeyNotFoundException($"Item {itemId} not found");
        
        if(newState == ItemState.Defected)
            throw new InvalidOperationException($"Can't change state to defected. Use specialized /items/{itemId}/defect endpoint.");
        
        _dbContext.Entry(item).Property(i => i.State).CurrentValue = newState;
        await _dbContext.SaveChangesAsync();
        return item;
    }

    public async Task DeleteItem(int itemId)
    {
        var item = await GetItemAsync(itemId);
        if (item == null) return;

        TryDeleteItemImage(item.ImageName);

        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<DeliveriesAnalytics> GetAnalytics(int days)
    {
        DateTime start = DateTime.UtcNow.AddDays(-days);
        DateTime end = DateTime.UtcNow;

        var items = await _dbContext.Items
            .AsNoTracking()
            .Where(x => x.ReceivedDate >= start && x.ReceivedDate <= end)
            .ToListAsync();
        
        var mostFrequentCategory = items
            .GroupBy(i => i.CategoryId)
            .Select(g => new 
            { 
                CategoryId = g.Key, 
                Count = g.Count() 
            })
            .OrderByDescending(g => g.Count)
            .FirstOrDefault();
        
        return new DeliveriesAnalytics(items.Count, mostFrequentCategory.CategoryId);
    }
}