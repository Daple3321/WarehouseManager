using Microsoft.EntityFrameworkCore;
using WarehouseManager.Data;
using WarehouseManager.Models.DTOs;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Services;

public interface IDefectService
{
    Task<DefectReport> CreateReport(DefectReportDto reportDto);
    Task<DefectReport> GetReport(int reportId);
    Task<FileStream?> GetImageForReport(int reportId);
    Task<DefectsAnalytics> GetAnalytics(int days = 7);
}

public class DefectService : IDefectService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DefectService> _logger;
    
    private static readonly string StorageFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _imagesPath = Path.Combine(StorageFolderPath, "DefectImages");
    private readonly string _reportsPath = Path.Combine(StorageFolderPath, "DefectReports");
    private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png"};
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB
    
    public DefectService(AppDbContext dbContext, ILogger<DefectService> logger)
    {
        _context = dbContext;
        _logger = logger;
        
        if (!Directory.Exists(_imagesPath))
            Directory.CreateDirectory(_imagesPath);
        
        if (!Directory.Exists(_reportsPath))
            Directory.CreateDirectory(_reportsPath);
    }

    public async Task<DefectReport> CreateReport(DefectReportDto reportDto)
    {
        if(string.IsNullOrEmpty(reportDto.DefectReason))
            throw new InvalidOperationException("Defect reason not specified");
        
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == reportDto.ItemId)
                   ?? throw new KeyNotFoundException($"Item {reportDto.ItemId} not found");

        var imagePath = await TryUploadImage(reportDto.DefectImage);

        var report = new DefectReport(imagePath, reportDto.DefectReason, reportDto.ItemId, DateTime.UtcNow, 0);

        _context.Reports.Add(report);
        await _context.SaveChangesAsync();

        return report;
    }

    public async Task<DefectReport> GetReport(int reportId)
    {
        if(reportId < 0) throw new InvalidOperationException("Id can't be negative");
        
        var report = await _context.Reports.FirstOrDefaultAsync(i => i.Id == reportId)
                   ?? throw new KeyNotFoundException($"Report {reportId} not found");

        return report;
    }
    
    public async Task<FileStream?> GetImageForReport(int reportId)
    {
        var report = await _context.Reports.FirstOrDefaultAsync(i => i.Id == reportId)
                   ?? throw new KeyNotFoundException($"Report {reportId} not found");
        
        if(string.IsNullOrEmpty(report.DefectImageGuid)) return null;
        
        var fullPath = Path.Combine(_imagesPath, report.DefectImageGuid);
        var fileStream = File.OpenRead(fullPath);

        return fileStream;
    }
    
    
    // public async Task<PagedResult<DefectReport>> GetReports()
    // {
    //     
    // }

    private async Task<string?> TryUploadImage(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            throw new InvalidOperationException("No file uploaded.");

        if (imageFile.Length > MaxFileSize)
            throw new InvalidOperationException("File size exceeds the 5 MB limit.");

        var extension = Path.GetExtension(imageFile.FileName).ToLower();
        if (!_allowedExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid file type.");
        
        var trustedFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_imagesPath, trustedFileName);

        try
        {
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when uploading item image");
            throw;
        }

        return trustedFileName;
    }
    
    private void TryDeleteItemImage(string imageName)
    {
        if(string.IsNullOrEmpty(imageName)) return;
        
        var fullPath = Path.Combine(_imagesPath, imageName);
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Image {name} successfully deleted.", imageName);
            }
            else
            {
                _logger.LogWarning("File not found.");
            }
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("Error: You do not have permission to delete this file or it is read-only.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Error: The file is locked by another process. Details: {msg}", ex.Message);
        }
    }
    
    // this could be a separate interface like IAnalyticsProvider
    public async Task<DefectsAnalytics> GetAnalytics(int days = 7)
    {
        DateTime start = DateTime.UtcNow.AddDays(-days);
        DateTime end = DateTime.UtcNow;

        var reports = await _context.Reports.AsNoTracking().ToListAsync();
        var count = reports.Count(x => x.CreationTime >= start && x.CreationTime <= end);
        
        var mostFrequentCategory = await _context.Items
            .GroupBy(i => i.CategoryId)
            .Select(g => new 
            { 
                CategoryId = g.Key, 
                Count = g.Count() 
            })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync();
        
        
        return new DefectsAnalytics(count, mostFrequentCategory.CategoryId, mostFrequentCategory.Count);
    }
}