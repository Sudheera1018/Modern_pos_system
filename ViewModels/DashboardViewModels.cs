namespace ModernPosSystem.ViewModels;

public class DashboardViewModel
{
    public List<SummaryCardViewModel> Cards { get; set; } = [];

    public List<ChartSeriesViewModel> DailySalesTrend { get; set; } = [];

    public List<ChartSeriesViewModel> CategorySalesSummary { get; set; } = [];

    public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = [];

    public List<LowStockItemViewModel> LowStockProducts { get; set; } = [];

    public List<RecentSaleViewModel> RecentSales { get; set; } = [];
}

public class ChartSeriesViewModel
{
    public string Label { get; set; } = string.Empty;

    public decimal Value { get; set; }
}

public class TopSellingProductViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public decimal QuantitySold { get; set; }

    public decimal Revenue { get; set; }
}

public class LowStockItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public decimal CurrentStock { get; set; }

    public decimal ReorderLevel { get; set; }
}

public class RecentSaleViewModel
{
    public string InvoiceNo { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CashierName { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    public decimal GrandTotal { get; set; }
}
