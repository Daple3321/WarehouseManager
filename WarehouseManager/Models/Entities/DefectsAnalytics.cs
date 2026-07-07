namespace WarehouseManager.Models.Entities;

public class DefectsAnalytics(int amount, int mostDefectedCategoryId, int mostDefectedCategoryCount)
{
    public int DefectsAmount { get; set; } = amount;
    public int MostDefectedCategoryId { get; set; } = mostDefectedCategoryId;
    public int MostDefectedCategoryCount { get; set; } = mostDefectedCategoryCount;
}