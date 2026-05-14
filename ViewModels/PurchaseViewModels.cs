using System.ComponentModel.DataAnnotations;

namespace ModernPosSystem.ViewModels;

public class PurchasePageViewModel
{
    public PurchaseFormViewModel Form { get; set; } = new();

    public List<PurchaseListItemViewModel> Purchases { get; set; } = [];
}

public class PurchaseFormViewModel
{
    public string? Id { get; set; }

    public string GrnNumber { get; set; } = string.Empty;

    [Required]
    public string SupplierId { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime GrnDate { get; set; } = DateTime.Today;

    public string Notes { get; set; } = string.Empty;

    public List<PurchaseItemFormViewModel> Items { get; set; } = [new()];

    public List<LookupOptionViewModel> Suppliers { get; set; } = [];

    public List<LookupOptionViewModel> Products { get; set; } = [];
}

public class PurchaseItemFormViewModel
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(0.01, 999999)]
    public decimal Quantity { get; set; }

    [Range(0, 9999999)]
    public decimal CostPrice { get; set; }
}

public class PurchaseListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string GrnNumber { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public DateTime GrnDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string ReceivedByName { get; set; } = string.Empty;
}

public class PurchaseDetailViewModel
{
    public string GrnNumber { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public DateTime GrnDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string Notes { get; set; } = string.Empty;

    public List<PurchaseDetailItemViewModel> Items { get; set; } = [];
}

public class PurchaseDetailItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal CostPrice { get; set; }

    public decimal LineTotal { get; set; }
}
