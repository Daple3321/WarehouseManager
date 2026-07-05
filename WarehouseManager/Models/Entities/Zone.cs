namespace WarehouseManager.Models.Entities;

public record Zone(int Id, string Name, int MaxItems)
{
    //public ICollection<Item>? Items { get; set; } = new List<Item>();
}