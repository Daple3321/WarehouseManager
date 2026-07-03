namespace WarehouseManager.Models.Entities;

public class PagedResult<T>(List<T> items, int count, int pageNumber, int pageSize)
{
    public List<T> Items { get; set; } = items;
    public int CurrentPage { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public int TotalItems { get; set; } = count;
    
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}