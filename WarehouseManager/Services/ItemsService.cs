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
    Task<FileStream?> GetImageForItem(int itemId);
    
    Task<Item> AddItemAsync(ItemDto item);
    Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items);
    
    Task<Item> MoveItemAsync(int itemId, int zoneId);
    
    Task DeleteItem(int itemId);
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
        
        _logger.LogInformation("LocalApplicationData path: {path}", StorageFolderPath);
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
        _logger.LogInformation("StoragePath: {path}", _storagePath);
        
    }
    
    public async Task<Item?> GetItemAsync(int itemId)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
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


    public async Task<PagedResult<Item>> GetItemsPaginatedAsync(int page, int pageSize)
    {
        int itemsToSkip = (page - 1) * pageSize;
        int totalItems = await _dbContext.Items.CountAsync();
        
        var items = await _dbContext.Items
            .Skip(itemsToSkip)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync();

        return new PagedResult<Item>(items, totalItems, page, pageSize);
    }
    
    public async Task<Item> AddItemAsync(ItemDto item)
    {
        var imageName = await TryUploadItemImage(item.ImageFile); 
        
        var newItem = new Item(item.ItemName, item.Description, ItemState.Received, 0, item.ZoneId, imageName);
        _dbContext.Items.Add(newItem);
        await _dbContext.SaveChangesAsync();

        return newItem;
    }
    
    public async Task<IEnumerable<Item>> AddItemsAsync(List<ItemDto> items)
    {
        var newItems = new List<Item>(items.Count);
        
        foreach (var item in items)
        {
            var imagePath = await TryUploadItemImage(item.ImageFile);
            // if (!string.IsNullOrEmpty(imagePath))
            // {
            //     
            // }

            newItems.Add(new Item(
                item.ItemName,
                item.Description, 
                ItemState.Received, 
                0, 
                item.ZoneId, 
                imagePath)
            );
        }
        
        // var newItems = items.Select(dto => new Item(
        //     dto.ItemName, 
        //     dto.Description, 
        //     ItemState.Inspection,
        //     0, 
        //     dto.ZoneId))
        //     .ToList();
        
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
            Console.WriteLine($"Error when uploading item image: {ex}");
            throw;
        }

        return trustedFileName;
    }

    private async Task TryDeleteItemImage(string imageName)
    {
        if(string.IsNullOrEmpty(imageName)) return;
        
        var fullPath = Path.Combine(_storagePath, imageName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                Console.WriteLine($"Image {imageName} successfully deleted.");
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: You do not have permission to delete this file or it is read-only.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error: The file is locked by another process. Details: {ex.Message}");
        }
    }

    public async Task<Item> MoveItemAsync(int itemId, int zoneId)
    {
        var item = await _dbContext.Items.FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new KeyNotFoundException($"Item {itemId} not found");

        if (item.ZoneId == zoneId)
            throw new InvalidOperationException($"Cannot move item to zone: {zoneId}. It is already in it.");
        
        if (item.State == ItemState.Defected)
            throw new InvalidOperationException("Cannot move a defected item");

        _dbContext.Entry(item).Property(i => i.ZoneId).CurrentValue = zoneId;
        await _dbContext.SaveChangesAsync();
        return item with { ZoneId = zoneId };
    }

    public async Task DeleteItem(int itemId)
    {
        var item = await GetItemAsync(itemId);
        if (item == null) return;

        await TryDeleteItemImage(item.ImageName);

        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync();
    }
}