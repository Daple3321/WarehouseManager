using Microsoft.AspNetCore.Mvc;
using Moq;
using WarehouseManager.Controllers;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;
using Xunit;

namespace WarehouseManager.Tests;

public class ItemsControllerTests
{
    private static (ItemsController controller, Mock<IItemService> itemSvc, Mock<IZoneService> zoneSvc) Build()
    {
        var itemSvc = new Mock<IItemService>();
        var zoneSvc = new Mock<IZoneService>();
        var defectSvc = new Mock<IDefectService>();
        return (new ItemsController(itemSvc.Object, zoneSvc.Object, defectSvc.Object), itemSvc, zoneSvc);
    }

    // --- GET /items/{id} ---

    [Fact]
    public async Task GetItemById_returns_bad_request_for_negative_id()
    {
        var (controller, _, _) = Build();
        var result = await controller.GetItemById(-1);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetItemById_returns_ok_when_item_exists()
    {
        var (controller, itemSvc, _) = Build();
        var item = new Item("Widget", "desc", ItemState.Received, 1, 1, 1, DateTime.UtcNow);
        itemSvc.Setup(s => s.GetItemAsync(1)).ReturnsAsync(item);

        var result = await controller.GetItemById(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // --- POST /items/receive ---

    [Fact]
    public async Task ReceiveItems_returns_bad_request_for_empty_list()
    {
        var (controller, _, _) = Build();
        var result = await controller.ReceiveItems([]);
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ReceiveItems_returns_created_when_valid()
    {
        var (controller, itemSvc, _) = Build();
        var dto = new ItemDto("Box", null, 1, 1);
        var created = new Item("Box", null, ItemState.Received, 1, 1, 1, DateTime.UtcNow);
        itemSvc.Setup(s => s.AddItemsAsync(It.IsAny<List<ItemDto>>())).ReturnsAsync([created]);

        var result = await controller.ReceiveItems([dto]);

        Assert.IsType<CreatedResult>(result);
    }

    // --- PUT /items/move ---

    [Fact]
    public async Task MoveItem_returns_conflict_when_zone_is_full()
    {
        var (controller, _, zoneSvc) = Build();
        var zone = new Zone(2, "B", 10);
        zoneSvc.Setup(z => z.GetItemCountInZone(2)).ReturnsAsync((10, zone));

        var result = await controller.MoveItem(new MoveDto(1, 2));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task MoveItem_returns_ok_when_zone_has_capacity()
    {
        var (controller, itemSvc, zoneSvc) = Build();
        var zone = new Zone(2, "B", 10);
        var moved = new Item("Widget", "desc", ItemState.Received, 1, 2, 1, DateTime.UtcNow);
        zoneSvc.Setup(z => z.GetItemCountInZone(2)).ReturnsAsync((5, zone));
        itemSvc.Setup(s => s.MoveItemAsync(1, 2)).ReturnsAsync(moved);

        var result = await controller.MoveItem(new MoveDto(1, 2));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // --- PUT /items/{id}/state/{state} ---

    [Fact]
    public async Task ChangeItemState_returns_ok_with_updated_item()
    {
        var (controller, itemSvc, _) = Build();
        var item = new Item("Widget", "desc", ItemState.ReadyForSale, 1, 1, 1, DateTime.UtcNow);
        itemSvc.Setup(s => s.ChangeItemState(1, ItemState.ReadyForSale)).ReturnsAsync(item);

        var result = await controller.ChangeItemState(1, ItemState.ReadyForSale);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(ItemState.ReadyForSale, ((Item)ok.Value!).State);
    }
}
