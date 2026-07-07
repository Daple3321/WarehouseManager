namespace WarehouseManager.Models.Entities;

public class DeliveriesAnalytics(int itemsReceived, int mostFrequentCategory)
{
    public int ItemsReceived { get; set; } = itemsReceived;
    public int MostFrequentCategory { get; set; } = mostFrequentCategory;
}