using Microsoft.EntityFrameworkCore;
using WarehouseManager.Models.Entities;

namespace WarehouseManager.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items { get; set; }
}
