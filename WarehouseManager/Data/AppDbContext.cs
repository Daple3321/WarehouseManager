using Microsoft.EntityFrameworkCore;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<DefectReport> Reports { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasOne(x => x.Zone).WithMany()
                .HasForeignKey(x => x.ZoneId);

            entity.HasOne(x => x.Category).WithMany()
                .HasForeignKey(x => x.CategoryId);
            
            // <CategoryId, ItemState> composite index
            entity.HasIndex(i => new { i.CategoryId, i.State })
                .HasDatabaseName("IDX_Items_CategoryId_State");
        });
        
        modelBuilder.Entity<Zone>().HasData(
            new Zone(1, "Sorting Zone", 25),
            new Zone(2, "Inspection Zone", 100),
            new Zone(3, "Defection Zone", 5),
            new Zone(4, "Ready for sale zone", 25)
        );
        
        modelBuilder.Entity<Category>().HasData(
            new Category(1, "Electronic Parts"),
            new Category(2, "Books"),
            new Category(3, "PC Parts")
        );
    }
}
