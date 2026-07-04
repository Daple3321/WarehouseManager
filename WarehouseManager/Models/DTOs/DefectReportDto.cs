namespace WarehouseManager.Models.DTOs;

public record DefectReportDto(IFormFile DefectImage, string DefectReason, int ItemId);