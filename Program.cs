using Microsoft.EntityFrameworkCore;
using pm.Data;
using pm.Models;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL UTC timestamp hatası için
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Add services to the container.
builder.Services.AddControllersWithViews();

// PostgreSQL veritabanı bağlantısı (Railway)
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? Environment.GetEnvironmentVariable("DATABASE_PUBLIC_URL");

if (!string.IsNullOrEmpty(databaseUrl) && databaseUrl.StartsWith("postgresql://"))
{
    // Railway PostgreSQL formatını dönüştür
    var uri = new Uri(databaseUrl);
    var username = uri.UserInfo.Split(':')[0];
    var password = uri.UserInfo.Split(':')[1];
    var connectionString = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Trim('/')};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // Fallback to appsettings.json
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Session ekle
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Veritabanını oluştur ve migration'ları uygula
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Veritabanı migration başlatılıyor...");
        context.Database.Migrate();
        logger.LogInformation("Migration tamamlandı!");
        
        if (!context.Products.Any())
        {
            logger.LogInformation("Seed data ekleniyor...");
            
            // Bölümler
        var sections = new[]
        {
            new Section { Name = "A", Description = "Motor Parçaları" },
            new Section { Name = "B", Description = "Hidrolik Sistem" },
            new Section { Name = "C", Description = "Şanzıman" }
        };
        context.Sections.AddRange(sections);
        context.SaveChanges();
        
        // Raflar
        var shelves = new[]
        {
            new Shelf { Name = "1. Raf", SectionId = sections[0].Id },
            new Shelf { Name = "2. Raf", SectionId = sections[0].Id },
            new Shelf { Name = "1. Raf", SectionId = sections[1].Id },
            new Shelf { Name = "2. Raf", SectionId = sections[1].Id },
            new Shelf { Name = "1. Raf", SectionId = sections[2].Id },
            new Shelf { Name = "2. Raf", SectionId = sections[2].Id }
        };
        context.Shelves.AddRange(shelves);
        context.SaveChanges();
        
        // CAT Ürünleri
        var products = new[]
        {
            new Product 
            { 
                ProductCode = "CAT-123456",
                Name = "Yağ Filtresi 1R-0750",
                SectionId = sections[0].Id,
                ShelfId = shelves[0].Id,
                Stock = 25,
                MinimumStock = 10,
                Price = 450.00m,
                Description = "CAT motor yağ filtresi"
            },
            new Product 
            { 
                ProductCode = "CAT-234567",
                Name = "Hava Filtresi 6I-2503",
                SectionId = sections[0].Id,
                ShelfId = shelves[0].Id,
                Stock = 15,
                MinimumStock = 5,
                Price = 850.00m,
                Description = "CAT hava filtresi"
            },
            new Product 
            { 
                ProductCode = "CAT-345678",
                Name = "Yakıt Filtresi 1R-0749",
                SectionId = sections[0].Id,
                ShelfId = shelves[1].Id,
                Stock = 30,
                MinimumStock = 8,
                Price = 520.00m,
                Description = "CAT yakıt filtresi"
            },
            new Product 
            { 
                ProductCode = "CAT-456789",
                Name = "Hidrolik Pompa 9T-4500",
                SectionId = sections[1].Id,
                ShelfId = shelves[2].Id,
                Stock = 5,
                MinimumStock = 2,
                Price = 15500.00m,
                Description = "CAT hidrolik pompa"
            },
            new Product 
            { 
                ProductCode = "CAT-567890",
                Name = "Hidrolik Yağ Filtresi 1R-1808",
                SectionId = sections[1].Id,
                ShelfId = shelves[3].Id,
                Stock = 20,
                MinimumStock = 6,
                Price = 680.00m,
                Description = "CAT hidrolik yağ filtresi"
            },
            new Product 
            { 
                ProductCode = "CAT-678901",
                Name = "Şanzıman Filtresi 3I-0106",
                SectionId = sections[2].Id,
                ShelfId = shelves[4].Id,
                Stock = 12,
                MinimumStock = 4,
                Price = 920.00m,
                Description = "CAT şanzıman filtresi"
            },
            new Product 
            { 
                ProductCode = "CAT-789012",
                Name = "Şanzıman Contası 6V-4133",
                SectionId = sections[2].Id,
                ShelfId = shelves[5].Id,
                Stock = 8,
                MinimumStock = 3,
                Price = 350.00m,
                Description = "CAT şanzıman contası"
            }
        };
        context.Products.AddRange(products);
        context.SaveChanges();
        
        // Stok hareketleri
        var movements = products.Select((p, i) => new StockMovement
        {
            ProductId = p.Id,
            Quantity = p.Stock,
            MovementType = "Giriş",
            MovementDate = DateTime.UtcNow.AddDays(-(5 - i / 2)),
            Notes = "İlk stok girişi"
        }).ToArray();
        context.StockMovements.AddRange(movements);
        context.SaveChanges();
        
        logger.LogInformation("Seed data başarıyla eklendi!");
    }
    else
    {
        logger.LogInformation("Veritabanı zaten dolu, seed data atlanıyor.");
    }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migration/seed hatası!");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Railway deployment için HTTPS redirection devre dışı
// app.UseHttpsRedirection();
app.UseRouting();

// Session kullan
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
