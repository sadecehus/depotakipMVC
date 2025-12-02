using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pm.Data;
using pm.Models;

namespace pm.Controllers;

public class ProductController : Controller
{
    private readonly AppDbContext _context;

    public ProductController(AppDbContext context)
    {
        _context = context;
    }

    // Bölümleri getir veya oluştur
    [HttpGet]
    public async Task<IActionResult> GetSections()
    {
        var sections = await _context.Sections.ToListAsync();
        return Json(sections);
    }

    // Bölüme göre rafları getir
    [HttpGet]
    public async Task<IActionResult> GetShelves(int sectionId)
    {
        var shelves = await _context.Shelves
            .Where(s => s.SectionId == sectionId)
            .ToListAsync();
        return Json(shelves);
    }

    // Bölüm oluştur
    [HttpPost]
    public async Task<IActionResult> CreateSection([FromBody] Section section)
    {
        // Aynı isimde bölüm var mı kontrol et
        var existing = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name == section.Name);
        
        if (existing != null)
            return Json(existing);

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();
        return Json(section);
    }

    // Raf oluştur
    [HttpPost]
    public async Task<IActionResult> CreateShelf([FromBody] Shelf shelf)
    {
        // Aynı bölümde aynı isimde raf var mı kontrol et
        var existing = await _context.Shelves
            .FirstOrDefaultAsync(s => s.Name == shelf.Name && s.SectionId == shelf.SectionId);
        
        if (existing != null)
            return Json(existing);

        _context.Shelves.Add(shelf);
        await _context.SaveChangesAsync();
        return Json(shelf);
    }

    // Tüm ürünleri getir
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _context.Products
            .Include(p => p.Section)
            .Include(p => p.Shelf)
            .Select(p => new
            {
                p.Id,
                p.ProductCode,
                p.Name,
                SectionName = p.Section!.Name,
                ShelfName = p.Shelf!.Name,
                p.Stock,
                p.MinimumStock,
                p.Price,
                p.Description
            })
            .ToListAsync();
        
        return Json(products);
    }

    // Ürün ekle
    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] ProductDto productDto)
    {
        // Bölüm kontrol et veya oluştur
        var section = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name == productDto.SectionName);
        
        if (section == null)
        {
            section = new Section { Name = productDto.SectionName };
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
        }

        // Raf kontrol et veya oluştur
        var shelf = await _context.Shelves
            .FirstOrDefaultAsync(s => s.Name == productDto.ShelfName && s.SectionId == section.Id);
        
        if (shelf == null)
        {
            shelf = new Shelf { Name = productDto.ShelfName, SectionId = section.Id };
            _context.Shelves.Add(shelf);
            await _context.SaveChangesAsync();
        }

        var product = new Product
        {
            ProductCode = productDto.ProductCode,
            Name = productDto.Name,
            SectionId = section.Id,
            ShelfId = shelf.Id,
            Stock = productDto.Stock,
            MinimumStock = productDto.MinimumStock,
            Price = productDto.Price,
            Description = productDto.Description
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // İlk stok girişi varsa kaydet
        if (productDto.Stock > 0)
        {
            var movement = new StockMovement
            {
                ProductId = product.Id,
                Quantity = productDto.Stock,
                MovementType = "Giriş",
                MovementDate = DateTime.Now,
                Notes = "İlk stok girişi"
            };
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true, message = "Ürün başarıyla eklendi" });
    }

    // Ürün sil
    [HttpDelete]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return Json(new { success = false, message = "Ürün bulunamadı" });

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Ürün silindi" });
    }

    // Ürün güncelle
    [HttpPost]
    public async Task<IActionResult> UpdateProduct([FromBody] UpdateProductDto updateDto)
    {
        var product = await _context.Products.FindAsync(updateDto.Id);
        if (product == null)
            return Json(new { success = false, message = "Ürün bulunamadı" });

        // Bölüm kontrol et veya oluştur
        var section = await _context.Sections
            .FirstOrDefaultAsync(s => s.Name == updateDto.SectionName);
        
        if (section == null)
        {
            section = new Section { Name = updateDto.SectionName };
            _context.Sections.Add(section);
            await _context.SaveChangesAsync();
        }

        // Raf kontrol et veya oluştur
        var shelf = await _context.Shelves
            .FirstOrDefaultAsync(s => s.Name == updateDto.ShelfName && s.SectionId == section.Id);
        
        if (shelf == null)
        {
            shelf = new Shelf { Name = updateDto.ShelfName, SectionId = section.Id };
            _context.Shelves.Add(shelf);
            await _context.SaveChangesAsync();
        }

        // Ürün bilgilerini güncelle
        product.ProductCode = updateDto.ProductCode;
        product.Name = updateDto.Name;
        product.SectionId = section.Id;
        product.ShelfId = shelf.Id;
        product.Stock = updateDto.Stock;
        product.MinimumStock = updateDto.MinimumStock;
        product.Price = updateDto.Price;
        product.Description = updateDto.Description;

        await _context.SaveChangesAsync();
        
        return Json(new { success = true, message = "Ürün başarıyla güncellendi" });
    }

    // Stok hareketlerini getir
    [HttpGet]
    public async Task<IActionResult> GetStockMovements()
    {
        var movements = await _context.StockMovements
            .Include(m => m.Product)
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new
            {
                m.Id,
                m.MovementDate,
                ProductCode = m.Product!.ProductCode,
                ProductName = m.Product.Name,
                m.MovementType,
                m.Quantity,
                m.UnitPrice,
                m.TotalPrice,
                m.Notes
            })
            .ToListAsync();
        
        return Json(movements);
    }

    // Ürün satışı
    [HttpPost]
    public async Task<IActionResult> SellProduct([FromBody] SellProductDto sellDto)
    {
        var product = await _context.Products.FindAsync(sellDto.ProductId);
        if (product == null)
            return Json(new { success = false, message = "Ürün bulunamadı" });

        if (product.Stock < sellDto.Quantity)
            return Json(new { success = false, message = "Yetersiz stok! Mevcut stok: " + product.Stock });

        // Stoktan düş
        product.Stock -= sellDto.Quantity;
        
        // Satış hareketini kaydet
        var totalPrice = sellDto.Quantity * sellDto.UnitPrice;
        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = sellDto.Quantity,
            MovementType = "Çıkış",
            MovementDate = DateTime.Now,
            UnitPrice = sellDto.UnitPrice,
            TotalPrice = totalPrice,
            Notes = sellDto.Notes ?? "Ürün satışı"
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"{sellDto.Quantity} adet ürün satışı yapıldı. Toplam: {totalPrice:F2} ₺" });
    }

    // Varolan ürüne stok ekle
    [HttpPost]
    public async Task<IActionResult> AddStock([FromBody] AddStockDto addStockDto)
    {
        var product = await _context.Products.FindAsync(addStockDto.ProductId);
        if (product == null)
            return Json(new { success = false, message = "Ürün bulunamadı" });

        // Stok ekle
        product.Stock += addStockDto.Quantity;
        
        // Stok giriş hareketini kaydet
        var totalPrice = addStockDto.Quantity * addStockDto.UnitPrice;
        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = addStockDto.Quantity,
            MovementType = "Giriş",
            MovementDate = DateTime.Now,
            UnitPrice = addStockDto.UnitPrice,
            TotalPrice = totalPrice,
            Notes = addStockDto.Notes ?? "Stok girişi"
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"{addStockDto.Quantity} adet stok eklendi. Yeni stok: {product.Stock}" });
    }

    // Stok güncelle
    [HttpPost]
    public async Task<IActionResult> UpdateStock([FromBody] StockUpdateDto stockUpdate)
    {
        var product = await _context.Products.FindAsync(stockUpdate.ProductId);
        if (product == null)
            return Json(new { success = false, message = "Ürün bulunamadı" });

        product.Stock += stockUpdate.Quantity;
        
        var movement = new StockMovement
        {
            ProductId = product.Id,
            Quantity = Math.Abs(stockUpdate.Quantity),
            MovementType = stockUpdate.Quantity > 0 ? "Giriş" : "Çıkış",
            MovementDate = DateTime.Now,
            Notes = stockUpdate.Notes
        };

        _context.StockMovements.Add(movement);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Stok güncellendi" });
    }
}

// DTO'lar
public class ProductDto
{
    public required string ProductCode { get; set; }
    public required string Name { get; set; }
    public required string SectionName { get; set; }
    public required string ShelfName { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
}

public class SellProductDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class AddStockDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}

public class StockUpdateDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}

public class UpdateProductDto
{
    public int Id { get; set; }
    public required string ProductCode { get; set; }
    public required string Name { get; set; }
    public required string SectionName { get; set; }
    public required string ShelfName { get; set; }
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
}
