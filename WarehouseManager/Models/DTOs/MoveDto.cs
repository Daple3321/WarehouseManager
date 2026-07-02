using WarehouseManager.Models.Entities;

namespace WarehouseManager.Models.DTOs;

/// <summary>
/// 
/// </summary>
/// <param name="ItemId">Item to move</param>
/// <param name="ZoneId">Zone to move item to</param>
/// <param name="NewState">New state of item</param>
public record MoveDto(int ItemId, int ZoneId, ItemState NewState);