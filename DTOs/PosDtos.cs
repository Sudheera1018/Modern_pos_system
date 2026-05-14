using System.ComponentModel.DataAnnotations;
using ModernPosSystem.Models;

namespace ModernPosSystem.DTOs;

public class CheckoutRequestDto
{
    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public bool IsWalkInCustomer { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Range(0, 9999999)]
    public decimal TaxAmount { get; set; }

    [Range(0, 9999999)]
    public decimal RedeemedLoyaltyPoints { get; set; }

    public bool UseMaxLoyaltyPoints { get; set; }

    [Required]
    public CheckoutPaymentBreakdownDto Payment { get; set; } = new();

    public string Notes { get; set; } = string.Empty;

    public List<CheckoutLineItemDto> Items { get; set; } = [];
}

public class CheckoutPaymentBreakdownDto
{
    [Range(0, 9999999)]
    public decimal CashAmount { get; set; }

    [Range(0, 9999999)]
    public decimal CardAmount { get; set; }

    [Range(0, 9999999)]
    public decimal FundTransferAmount { get; set; }

    public string CardReference { get; set; } = string.Empty;

    public string TransferReferenceNo { get; set; } = string.Empty;

    public string TransferProvider { get; set; } = string.Empty;
}

public class CheckoutLineItemDto
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(0.01, 99999)]
    public decimal Quantity { get; set; }

    [Range(0, 9999999)]
    public decimal DiscountAmount { get; set; }
}
