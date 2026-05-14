namespace ModernPosSystem.Models;

[BsonCollection("Sales")]
public class Sale : BaseEntity
{
    public string InvoiceNo { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public bool IsWalkInCustomer { get; set; }

    public string CashierUserId { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal LoyaltyPointsRedeemed { get; set; }

    public decimal LoyaltyAmountRedeemed { get; set; }

    public decimal LoyaltyPointsEarned { get; set; }

    public decimal NetPayable { get; set; }

    public decimal AmountReceived { get; set; }

    public decimal CashPaidAmount { get; set; }

    public decimal CardPaidAmount { get; set; }

    public decimal FundTransferAmount { get; set; }

    public string CardReference { get; set; } = string.Empty;

    public string TransferReferenceNo { get; set; } = string.Empty;

    public string TransferProvider { get; set; } = string.Empty;

    public decimal BalanceReturn { get; set; }

    public string Notes { get; set; } = string.Empty;

    public List<SaleItem> Items { get; set; } = [];
}

public class SaleItem
{
    public string ProductId { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}
