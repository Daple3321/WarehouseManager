namespace WarehouseManager.Models.Entities;

public record DefectReport(string DefectImageGuid, string DefectReason, int ItemId, DateTime CreationTime, int Id);