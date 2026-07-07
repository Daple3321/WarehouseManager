using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WarehouseManager.Data;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IZoneService
{
    Task<List<Zone>> GetZones();
    Task<Zone?> GetZone(int zoneId);
    
    Task<Zone> AddZone(ZoneDto newZone);
    
    Task<PagedResult<Item>> GetItemsInZone(int zoneId, int page, int pageSize);
    Task<(int count, Zone zone)> GetItemCountInZone(int zoneId);
}

public class ZoneService(ILogger<ZoneService> logger, AppDbContext context, IDistributedCache cache) : IZoneService
{
    private readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
    
    private readonly DistributedCacheEntryOptions _cacheEntryOptions = new DistributedCacheEntryOptions()
        .SetAbsoluteExpiration(TimeSpan.FromHours(1))
        .SetSlidingExpiration(TimeSpan.FromMinutes(20));
    
    public async Task<List<Zone>> GetZones()
    {
        string cacheKey = "zones:all";
        string cachedData = await cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("Cache hit for all zones");
            return JsonSerializer.Deserialize<List<Zone>>(cachedData, _opts);
        }

        // Cache Miss
        List<Zone> zones = await context.Zones.ToListAsync();
        
        // caching
        string dataToCache = JsonSerializer.Serialize(zones);
        await cache.SetStringAsync(cacheKey, dataToCache, _cacheEntryOptions);

        return zones;
    }

    public async Task<Zone?> GetZone(int zoneId)
    {
        string cacheKey = $"zones:{zoneId}";
        string cachedData = await cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cachedData))
        {
            logger.LogInformation("Cache hit for zone = {id}", zoneId);
            return JsonSerializer.Deserialize<Zone>(cachedData, _opts);
        }
        
        var zone = await context.Zones
                       .FirstOrDefaultAsync(x => x.Id == zoneId)
                   ?? throw new KeyNotFoundException($"Zone {zoneId} not found");
        
        string dataToCache = JsonSerializer.Serialize(zone);
        await cache.SetStringAsync(cacheKey, dataToCache, _cacheEntryOptions);

        return zone;
    }

    public async Task<Zone> AddZone(ZoneDto newZone)
    {
        var zone = new Zone(0, newZone.Name, newZone.MaxItems);
        
        context.Zones.Add(zone);

        await context.SaveChangesAsync();
        
        string dataToCache = JsonSerializer.Serialize(zone);
        await cache.SetStringAsync($"zones:{zone.Id}", dataToCache, _cacheEntryOptions);
        
        // evict zones:all cache
        await cache.RemoveAsync("zones:all");

        return zone;
    }

    public async Task<PagedResult<Item>> GetItemsInZone(int zoneId, int page, int pageSize)
    {
        var zone = await context.Zones.FirstOrDefaultAsync(x => x.Id == zoneId)
            ?? throw new KeyNotFoundException($"Zone {zoneId} not found");
        
        int itemsToSkip = (page - 1) * pageSize;
        int totalItems = await context.Items.CountAsync();
        
        var items = await context.Items
            .Skip(itemsToSkip)
            .Take(pageSize)
            .Where(x => x.ZoneId == zoneId)
            .Include(x => x.Zone)
            .Include(x => x.Category)
            .ToListAsync();

        return new PagedResult<Item>(items, totalItems, page, pageSize);
    }

    public async Task<(int count, Zone zone)> GetItemCountInZone(int zoneId)
    {
        var zone = await context.Zones.FirstOrDefaultAsync(x => x.Id == zoneId)
                   ?? throw new KeyNotFoundException($"Zone {zoneId} not found");
        
        var count = await context.Items.CountAsync(x => x.ZoneId == zoneId);
        
        return (count, zone);
    }
}