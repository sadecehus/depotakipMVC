namespace pm.Models;

public class StockMovement
{
    public int Id { get; set; }
    public required int ProductId { get; set; }
    public required int Quantity { get; set; }
    public required string MovementType { get; set; } // "Giriş" veya "Çıkış"
    public DateTime MovementDate { get; set; }
    public decimal? UnitPrice { get; set; } // Birim fiyat
    public decimal? TotalPrice { get; set; } // Toplam fiyat
    public string? Notes { get; set; }
    
    public Product? Product { get; set; }
}
