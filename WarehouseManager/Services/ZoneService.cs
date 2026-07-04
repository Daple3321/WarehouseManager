using WarehouseManager.Data;

namespace WarehouseManager.Services;

public interface IZoneService
{
    
}

public class ZoneService(ILogger<ZoneService> logger, AppDbContext context) : IZoneService
{
    
}