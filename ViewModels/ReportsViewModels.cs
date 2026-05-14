namespace ModernPosSystem.ViewModels;

public class ReportsPageViewModel
{
    public ReportFilterViewModel Filter { get; set; } = new();

    public List<ReportBlockViewModel> Reports { get; set; } = [];
}

public class ReportFilterViewModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today;

    public DateTime? ToDate { get; set; } = DateTime.Today;

    public string? CashierUserId { get; set; }

    public string? ProductId { get; set; }

    public List<LookupOptionViewModel> Cashiers { get; set; } = [];

    public List<LookupOptionViewModel> Products { get; set; } = [];
}

public class ReportBlockViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ReportRowViewModel> Rows { get; set; } = [];
}

public class ReportRowViewModel
{
    public string Label { get; set; } = string.Empty;

    public string SecondaryLabel { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Amount { get; set; }
}
