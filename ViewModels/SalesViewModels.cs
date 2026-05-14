using System.ComponentModel.DataAnnotations;

namespace ModernPosSystem.ViewModels;

public class SalesHistoryPageViewModel
{
    [Display(Name = "Invoice No")]
    public string InvoiceNo { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? FromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; set; }

    public string? CashierUserId { get; set; }

    public List<LookupOptionViewModel> Cashiers { get; set; } = [];

    public List<SalesHistoryItemViewModel> Sales { get; set; } = [];
}

public class SalesHistoryItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string InvoiceNo { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public decimal GrandTotal { get; set; }
}
