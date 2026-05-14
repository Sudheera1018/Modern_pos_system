namespace ModernPosSystem.Models;

[BsonCollection("Products")]
public class Product : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SupplierId { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public decimal CostPrice { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal ReorderLevel { get; set; }

    public int ForecastItemCode { get; set; }

    public decimal? SafetyStock { get; set; }

    public int? SupplierLeadTimeDays { get; set; }

    public string Unit { get; set; } = "pcs";

    public string ImageUrl { get; set; } = string.Empty;

    public string? ImagePath { get; set; }
}
