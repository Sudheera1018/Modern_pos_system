using ModernPosSystem.Models;

namespace ModernPosSystem.ViewModels;

public class PosPageViewModel
{
    public string CashierName { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public string PreviewInvoiceNo { get; set; } = string.Empty;

    public string WalkInCustomerId { get; set; } = string.Empty;

    public List<PosProductCardViewModel> Products { get; set; } = [];

    public List<PosCustomerOptionViewModel> Customers { get; set; } = [];

    public List<PosPaymentMethodOptionViewModel> PaymentMethods { get; set; } = [];

    public decimal LoyaltyEarnRateAmount { get; set; } = 100;

    public decimal LoyaltyRedeemValue { get; set; } = 1;

    public decimal MinimumRedeemPoints { get; set; } = 1;
}

public class PosProductCardViewModel
{
    public string Id { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal ReorderLevel { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
}

public class InvoiceViewModel
{
    public string InvoiceNo { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

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

    public List<InvoiceItemViewModel> Items { get; set; } = [];
}

public class PosCustomerOptionViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public decimal LoyaltyPoints { get; set; }

    public bool IsWalkIn { get; set; }
}

public class PosPaymentMethodOptionViewModel
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public class LoyaltyPolicyViewModel
{
    public decimal EarnRateAmount { get; set; } = 100;

    public decimal RedeemValue { get; set; } = 1;

    public decimal MinimumRedeemPoints { get; set; } = 1;
}

public class InvoiceItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }
}
