using Microsoft.EntityFrameworkCore;
using pm.Models;

namespace pm.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Section> Sections { get; set; }
    public DbSet<Shelf> Shelves { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // İlk kullanıcıyı ekle (admin/admin)
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "adhald", Password = "onlinesm.1299" }
        );
    }
}
