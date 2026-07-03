using Microsoft.AspNetCore.Mvc;
using Moq;
using WarehouseManager.Controllers;
using WarehouseManager.Models.Entities;
using WarehouseManager.Services;
using Xunit;

namespace WarehouseManager.Tests;

public class ItemsControllerTests
{
    [Fact]
    public async Task Get_returns_not_found_when_service_returns_null()
    {
        var itemService = new Mock<IItemService>();
        itemService.Setup(s => s.GetItemAsync("missing")).ReturnsAsync((Item?)null);

        var controller = new ItemsController(itemService.Object);
        var result = await controller.Get("missing");

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
