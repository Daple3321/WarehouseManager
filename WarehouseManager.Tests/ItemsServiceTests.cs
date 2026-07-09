using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    private static ItemsService CreateService(AppDbContext db) =>
        new(db, NullLogger<ItemsService>.Instance, new Mock<IDistributedCache>().Object);

    private static Item SeedItem(AppDbContext db, int zoneId = 1, int categoryId = 1)
    {
        var item = new Item("Widget", "desc", ItemState.Received, 0, zoneId, categoryId, DateTime.UtcNow);
        db.Items.Add(item);
        db.SaveChanges();
        return item;
    }

    private static void SeedZoneAndCategory(AppDbContext db, int zoneId = 1, int categoryId = 1)
    {
        db.Zones.Add(new Zone(zoneId, "A", 100));
        db.Categories.Add(new Category(categoryId, "General"));
        db.SaveChanges();
    }

    // --- GetItemAsync ---

    [Fact]
    public async Task GetItemAsync_throws_when_item_missing()
    {
        await using var db = CreateDb(nameof(GetItemAsync_throws_when_item_missing));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService(db).GetItemAsync(999));
    }

    [Fact]
    public async Task GetItemAsync_returns_item_when_found()
    {
        await using var db = CreateDb(nameof(GetItemAsync_returns_item_when_found));
        SeedZoneAndCategory(db);
        var seeded = SeedItem(db);

        var result = await CreateService(db).GetItemAsync(seeded.Id);

        Assert.Equal("Widget", result!.ItemName);
    }

    // --- MoveItemAsync ---

    [Fact]
    public async Task MoveItemAsync_throws_when_item_missing()
    {
        await using var db = CreateDb(nameof(MoveItemAsync_throws_when_item_missing));
        db.Zones.Add(new Zone(1, "A", 100));
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService(db).MoveItemAsync(999, 1));
    }

    [Fact]
    public async Task MoveItemAsync_throws_when_zone_missing()
    {
        await using var db = CreateDb(nameof(MoveItemAsync_throws_when_zone_missing));
        var item = SeedItem(db, zoneId: 1);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService(db).MoveItemAsync(item.Id, 999));
    }

    [Fact]
    public async Task MoveItemAsync_throws_when_already_in_same_zone()
    {
        await using var db = CreateDb(nameof(MoveItemAsync_throws_when_already_in_same_zone));
        db.Zones.Add(new Zone(1, "A", 100));
        var item = SeedItem(db, zoneId: 1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(db).MoveItemAsync(item.Id, 1));
    }

    [Fact]
    public async Task MoveItemAsync_updates_zone_when_valid()
    {
        await using var db = CreateDb(nameof(MoveItemAsync_updates_zone_when_valid));
        db.Zones.Add(new Zone(1, "A", 100));
        db.Zones.Add(new Zone(2, "B", 100));
        await db.SaveChangesAsync();
        var item = SeedItem(db, zoneId: 1);

        var updated = await CreateService(db).MoveItemAsync(item.Id, 2);

        Assert.Equal(2, updated.ZoneId);
    }

    // --- ChangeItemState ---

    [Fact]
    public async Task ChangeItemState_throws_when_item_missing()
    {
        await using var db = CreateDb(nameof(ChangeItemState_throws_when_item_missing));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => CreateService(db).ChangeItemState(999, ItemState.Inspection));
    }

    [Fact]
    public async Task ChangeItemState_throws_when_new_state_is_defected()
    {
        await using var db = CreateDb(nameof(ChangeItemState_throws_when_new_state_is_defected));
        var item = SeedItem(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(db).ChangeItemState(item.Id, ItemState.Defected));
    }

    [Fact]
    public async Task ChangeItemState_updates_state_when_valid()
    {
        await using var db = CreateDb(nameof(ChangeItemState_updates_state_when_valid));
        var item = SeedItem(db);

        var updated = await CreateService(db).ChangeItemState(item.Id, ItemState.ReadyForSale);

        Assert.Equal(ItemState.ReadyForSale, updated.State);
    }

    // --- AddItemAsync ---

    [Fact]
    public async Task AddItemAsync_throws_when_category_not_found()
    {
        await using var db = CreateDb(nameof(AddItemAsync_throws_when_category_not_found));
        var service = CreateService(db);
        var dto = new Models.DTOs.ItemDto("X", null, ZoneId: 1, CategoryId: 42);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AddItemAsync(dto));
    }
}
