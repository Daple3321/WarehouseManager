namespace WarehouseManager.Models.DTOs;

public record ItemDto(
    string ItemName, 
    string? Description, 
    int ZoneId,
    int CategoryId,
    IFormFile? ImageFile = null
);