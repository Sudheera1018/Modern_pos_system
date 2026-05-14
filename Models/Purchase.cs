namespace ModernPosSystem.Models;

[BsonCollection("Purchases")]
public class Purchase : BaseEntity
{
    public string GrnNumber { get; set; } = string.Empty;

    public string SupplierId { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public DateTime GrnDate { get; set; } = DateTime.UtcNow;

    public decimal TotalAmount { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string ReceivedByUserId { get; set; } = string.Empty;

    public string ReceivedByName { get; set; } = string.Empty;

    public List<PurchaseItem> Items { get; set; } = [];
}

public class PurchaseItem
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal CostPrice { get; set; }

    public decimal LineTotal { get; set; }
}
