namespace WarehouseManager.Models.Entities;

public enum ItemState
{
    InTransit = 0,
    Received = 1,
    Inspection = 2,
    Defected = 3,
    ReadyForSale = 4,
}

public record Item(
    string ItemName,
    string Description,
    ItemState State,
    int Id,
    int ZoneId,
    string? ImageName = null
)
{
    public Zone? Zone { get; set; }
}