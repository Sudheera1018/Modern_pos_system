namespace ModernPosSystem.Models;

[BsonCollection("InventoryMovements")]
public class InventoryMovement : BaseEntity
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public StockMovementType MovementType { get; set; }

    public decimal Quantity { get; set; }

    public decimal BeforeStock { get; set; }

    public decimal AfterStock { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public string ReferenceNo { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
