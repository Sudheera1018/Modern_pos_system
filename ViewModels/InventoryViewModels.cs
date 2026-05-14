using System.ComponentModel.DataAnnotations;

namespace ModernPosSystem.ViewModels;

public class InventoryOverviewViewModel
{
    public List<ProductListItemViewModel> Products { get; set; } = [];

    public List<InventoryMovementViewModel> Movements { get; set; } = [];

    public List<ProductListItemViewModel> LowStockProducts { get; set; } = [];

    public List<ProductListItemViewModel> OutOfStockProducts { get; set; } = [];
}

public class InventoryMovementViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string MovementType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal BeforeStock { get; set; }

    public decimal AfterStock { get; set; }

    public string ReferenceType { get; set; } = string.Empty;

    public string ReferenceNo { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string UserName { get; set; } = string.Empty;
}

public class StockAdjustmentViewModel
{
    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    public string AdjustmentType { get; set; } = "Increase";

    [Range(0.01, 9999999)]
    public decimal Quantity { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public List<LookupOptionViewModel> Products { get; set; } = [];
}
