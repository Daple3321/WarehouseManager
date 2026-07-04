using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WarehouseManager.Data;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;
using Xunit;

namespace WarehouseManager.Tests;

public class ItemsServiceTests
{
    private static AppDbContext CreateDb(string dbName) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options);

    // [Fact]
    // public async Task MoveItemAsync_throws_when_item_missing()
    // {
    //     await using var db = CreateDb(nameof(MoveItemAsync_throws_when_item_missing));
    //     var service = new ItemsService(db, NullLogger<ItemsService>.Instance);
    //
    //     await Assert.ThrowsAsync<KeyNotFoundException>(() => service.MoveItemAsync(999, 2));
    // }
    //
    // [Fact]
    // public async Task MoveItemAsync_updates_zone_when_item_exists()
    // {
    //     await using var db = CreateDb(nameof(MoveItemAsync_updates_zone_when_item_exists));
    //     db.Items.Add(new Item("SomeItem", "desc", ItemState.Received, 1, 1));
    //     await db.SaveChangesAsync();
    //
    //     var service = new ItemsService(db, NullLogger<ItemsService>.Instance);
    //     var updated = await service.MoveItemAsync(1, 5);
    //
    //     Assert.Equal(5, updated.ZoneId);
    // }
}
