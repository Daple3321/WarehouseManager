using Microsoft.EntityFrameworkCore;
using WarehouseManager.Data;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IZoneService
{
    Task<List<Zone>> GetZones();
    Task<Zone> GetZone(int zoneId);
    
    Task AddZone(Zone newZone);
    
    Task<PagedResult<Item>> GetItemsInZone(int zoneId, int page, int pageSize);
    Task<int> GetItemCountInZone(int zoneId);
}

public class ZoneService(ILogger<ZoneService> logger, AppDbContext context) : IZoneService
{
    public async Task<List<Zone>> GetZones()
    {
        return await context.Zones
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Zone> GetZone(int zoneId)
    {
        var zone = await context.Zones
                       //.Include(zone => zone.Items)
                       .FirstOrDefaultAsync(x => x.Id == zoneId)
                   ?? throw new KeyNotFoundException($"Zone {zoneId} not found");

        return zone;
    }

    public async Task AddZone(Zone newZone)
    {
        context.Zones.Add(newZone);

        await context.SaveChangesAsync();
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

    public async Task<int> GetItemCountInZone(int zoneId)
    {
        return await context.Items.CountAsync(x => x.ZoneId == zoneId);
    }
}