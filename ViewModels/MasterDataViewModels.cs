using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ModernPosSystem.ViewModels;

public class ProductFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    public string ProductName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required]
    public string CategoryId { get; set; } = string.Empty;

    [Required]
    public string SupplierId { get; set; } = string.Empty;

    [Range(0, 9999999)]
    public decimal CostPrice { get; set; }

    [Range(0, 9999999)]
    public decimal SellingPrice { get; set; }

    [Range(0, 9999999)]
    public decimal CurrentStock { get; set; }

    [Range(0, 9999999)]
    public decimal ReorderLevel { get; set; }

    [Display(Name = "Forecast Item Code")]
    [Range(1, int.MaxValue, ErrorMessage = "Forecast item code must be greater than zero.")]
    public int ForecastItemCode { get; set; }

    [Display(Name = "Safety Stock")]
    [Range(0, 9999999)]
    public decimal? SafetyStock { get; set; }

    [Display(Name = "Supplier Lead Time (Days)")]
    [Range(0, 365)]
    public int? SupplierLeadTimeDays { get; set; }

    [Required]
    public string Unit { get; set; } = "pcs";

    [Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = string.Empty;

    [Display(Name = "Upload Product Image")]
    public IFormFile? ImageFile { get; set; }

    public bool IsActive { get; set; } = true;

    public List<LookupOptionViewModel> Categories { get; set; } = [];

    public List<LookupOptionViewModel> Suppliers { get; set; } = [];
}

public class ProductListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string Barcode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }

    public decimal ReorderLevel { get; set; }

    public int ForecastItemCode { get; set; }

    public bool IsActive { get; set; }

    public string StockStatus { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;
}

public class CategoryFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string CategoryId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category Name")]
    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class CategoryListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public bool IsActive { get; set; }
}

public class CustomerFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string CustomerCode { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    [Range(0, 999999)]
    public decimal LoyaltyPoints { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CustomerListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string CustomerCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public decimal LoyaltyPoints { get; set; }

    public bool IsActive { get; set; }
}

public class SupplierFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string SupplierCode { get; set; } = string.Empty;

    [Required]
    public string SupplierName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class SupplierListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string SupplierCode { get; set; } = string.Empty;

    public string SupplierName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
