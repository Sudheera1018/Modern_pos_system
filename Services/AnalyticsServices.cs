using ClosedXML.Excel;
using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ModernPosSystem.Services;

public class DashboardService(
    IRepository<Sale> saleRepository,
    IRepository<Product> productRepository,
    IRepository<Customer> customerRepository) : IDashboardService
{
    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        var today = DateTime.UtcNow.Date;
        var sales = await saleRepository.GetAllAsync(x => x.IsActive);
        var products = await productRepository.GetAllAsync(x => x.IsActive);
        var customers = await customerRepository.GetAllAsync(x => x.IsActive);
        var todaySales = sales.Where(x => x.SaleDate.Date == today).ToList();
        var lastSevenDays = Enumerable.Range(0, 7).Select(offset => today.AddDays(-6 + offset)).ToList();

        return new DashboardViewModel
        {
            Cards =
            [
                new() { Title = "Today Sales", Value = todaySales.Sum(x => x.GrandTotal).ToString("C"), Subtitle = "Live store revenue", Icon = "bi bi-currency-dollar", AccentClass = "accent-emerald" },
                new() { Title = "Invoices Today", Value = todaySales.Count.ToString(), Subtitle = "Completed checkouts", Icon = "bi bi-receipt", AccentClass = "accent-blue" },
                new() { Title = "Total Products", Value = products.Count.ToString(), Subtitle = "Active sellable items", Icon = "bi bi-box-seam", AccentClass = "accent-violet" },
                new() { Title = "Low Stock Items", Value = products.Count(x => x.CurrentStock > 0 && x.CurrentStock <= x.ReorderLevel).ToString(), Subtitle = "Need attention soon", Icon = "bi bi-exclamation-triangle", AccentClass = "accent-amber" },
                new() { Title = "Out of Stock", Value = products.Count(x => x.CurrentStock <= 0).ToString(), Subtitle = "Unavailable in POS", Icon = "bi bi-bag-x", AccentClass = "accent-red" },
                new() { Title = "Customers", Value = customers.Count.ToString(), Subtitle = "Active customer profiles", Icon = "bi bi-people", AccentClass = "accent-slate" }
            ],
            DailySalesTrend = lastSevenDays.Select(day => new ChartSeriesViewModel
            {
                Label = day.ToString("dd MMM"),
                Value = sales.Where(x => x.SaleDate.Date == day).Sum(x => x.GrandTotal)
            }).ToList(),
            CategorySalesSummary = sales
                .SelectMany(x => x.Items)
                .GroupBy(x => x.ProductName)
                .OrderByDescending(x => x.Sum(y => y.LineTotal))
                .Take(5)
                .Select(x => new ChartSeriesViewModel { Label = x.Key, Value = x.Sum(y => y.LineTotal) })
                .ToList(),
            TopSellingProducts = sales
                .SelectMany(x => x.Items)
                .GroupBy(x => x.ProductName)
                .OrderByDescending(x => x.Sum(y => y.Quantity))
                .Take(5)
                .Select(x => new TopSellingProductViewModel
                {
                    ProductName = x.Key,
                    QuantitySold = x.Sum(y => y.Quantity),
                    Revenue = x.Sum(y => y.LineTotal)
                }).ToList(),
            LowStockProducts = products
                .Where(x => x.CurrentStock > 0 && x.CurrentStock <= x.ReorderLevel)
                .Take(6)
                .Select(x => new LowStockItemViewModel
                {
                    ProductName = x.ProductName,
                    ProductCode = x.ProductCode,
                    CurrentStock = x.CurrentStock,
                    ReorderLevel = x.ReorderLevel
                }).ToList(),
            RecentSales = sales
                .OrderByDescending(x => x.SaleDate)
                .Take(6)
                .Select(x => new RecentSaleViewModel
                {
                    InvoiceNo = x.InvoiceNo,
                    CustomerName = x.CustomerName,
                    CashierName = x.CashierName,
                    SaleDate = x.SaleDate,
                    GrandTotal = x.GrandTotal
                }).ToList()
        };
    }
}

public class ReportService(
    ISalesService salesService,
    IInventoryService inventoryService,
    IRepository<Purchase> purchaseRepository) : IReportService
{
    public async Task<ReportsPageViewModel> GetReportsAsync(ReportFilterDto filter)
    {
        var sales = await salesService.GetSalesAsync(filter.FromDate, filter.ToDate);
        var purchases = await purchaseRepository.GetAllAsync(x => x.IsActive);
        var inventory = await inventoryService.GetOverviewAsync();

        if (!string.IsNullOrWhiteSpace(filter.CashierUserId))
        {
            sales = sales.Where(x => x.CashierUserId == filter.CashierUserId).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.ProductId))
        {
            sales = sales.Where(x => x.Items.Any(i => i.ProductId == filter.ProductId)).ToList();
        }

        return new ReportsPageViewModel
        {
            Reports =
            [
                new()
                {
                    Title = "Daily / Range Sales",
                    Description = "Sales totals across the selected date window.",
                    Rows =
                    [
                        new() { Label = "Total Sales", SecondaryLabel = $"{sales.Count} invoices", Quantity = sales.Count, Amount = sales.Sum(x => x.GrandTotal) },
                        new() { Label = "Discounts", SecondaryLabel = "Item and invoice discounts", Quantity = 0, Amount = sales.Sum(x => x.DiscountTotal) },
                        new() { Label = "Tax", SecondaryLabel = "Collected tax value", Quantity = 0, Amount = sales.Sum(x => x.TaxAmount) }
                    ]
                },
                new()
                {
                    Title = "Product Sales",
                    Description = "Top performing sold items.",
                    Rows = sales.SelectMany(x => x.Items).GroupBy(x => x.ProductName)
                        .OrderByDescending(x => x.Sum(y => y.Quantity))
                        .Take(8)
                        .Select(x => new ReportRowViewModel
                        {
                            Label = x.Key,
                            SecondaryLabel = "Units sold",
                            Quantity = x.Sum(y => y.Quantity),
                            Amount = x.Sum(y => y.LineTotal)
                        }).ToList()
                },
                new()
                {
                    Title = "Low Stock",
                    Description = "Products below reorder level.",
                    Rows = inventory.LowStockProducts.Select(x => new ReportRowViewModel
                    {
                        Label = x.ProductName,
                        SecondaryLabel = $"{x.CurrentStock} remaining / reorder {x.ReorderLevel}",
                        Quantity = x.CurrentStock,
                        Amount = 0
                    }).ToList()
                },
                new()
                {
                    Title = "Inventory Movement",
                    Description = "Recent inventory adjustments and stock flow.",
                    Rows = inventory.Movements.Take(10).Select(x => new ReportRowViewModel
                    {
                        Label = x.ProductName,
                        SecondaryLabel = $"{x.MovementType} - {x.ReferenceNo}",
                        Quantity = x.Quantity,
                        Amount = x.AfterStock
                    }).ToList()
                },
                new()
                {
                    Title = "Purchases / GRN",
                    Description = "Recent stock receipts.",
                    Rows = purchases.OrderByDescending(x => x.GrnDate).Take(8).Select(x => new ReportRowViewModel
                    {
                        Label = x.GrnNumber,
                        SecondaryLabel = x.SupplierName,
                        Quantity = x.Items.Sum(y => y.Quantity),
                        Amount = x.TotalAmount
                    }).ToList()
                },
                new()
                {
                    Title = "Cashier Sales",
                    Description = "Performance by cashier.",
                    Rows = sales.GroupBy(x => x.CashierName)
                        .Select(x => new ReportRowViewModel
                        {
                            Label = x.Key,
                            SecondaryLabel = "Invoices handled",
                            Quantity = x.Count(),
                            Amount = x.Sum(y => y.GrandTotal)
                        }).OrderByDescending(x => x.Amount).ToList()
                }
            ]
        };
    }

    public async Task<FileExportResult> ExportExcelAsync(ReportFilterDto filter)
    {
        var report = await GetReportsAsync(filter);
        using var workbook = new XLWorkbook();

        foreach (var block in report.Reports)
        {
            var sheet = workbook.Worksheets.Add(block.Title.Length > 31 ? block.Title[..31] : block.Title);
            sheet.Cell(1, 1).Value = block.Title;
            sheet.Cell(2, 1).Value = block.Description;
            sheet.Cell(4, 1).Value = "Label";
            sheet.Cell(4, 2).Value = "Secondary";
            sheet.Cell(4, 3).Value = "Quantity";
            sheet.Cell(4, 4).Value = "Amount";

            for (var i = 0; i < block.Rows.Count; i++)
            {
                var row = block.Rows[i];
                sheet.Cell(i + 5, 1).Value = row.Label;
                sheet.Cell(i + 5, 2).Value = row.SecondaryLabel;
                sheet.Cell(i + 5, 3).Value = row.Quantity;
                sheet.Cell(i + 5, 4).Value = row.Amount;
            }

            sheet.Columns().AdjustToContents();
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileExportResult
        {
            FileName = $"pos-reports-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            Content = stream.ToArray()
        };
    }

    public async Task<FileExportResult> ExportPdfAsync(ReportFilterDto filter)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var report = await GetReportsAsync(filter);
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);
                page.Size(PageSizes.A4);
                page.Header().Text("Modern POS Reports").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                page.Content().Column(column =>
                {
                    foreach (var block in report.Reports)
                    {
                        column.Item().PaddingBottom(10).Column(inner =>
                        {
                            inner.Item().Text(block.Title).FontSize(15).SemiBold();
                            inner.Item().Text(block.Description).FontSize(10).FontColor(Colors.Grey.Darken1);
                            inner.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(4);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Label").SemiBold();
                                    header.Cell().Text("Secondary").SemiBold();
                                    header.Cell().Text("Qty").SemiBold();
                                    header.Cell().Text("Amount").SemiBold();
                                });

                                foreach (var row in block.Rows)
                                {
                                    table.Cell().Text(row.Label);
                                    table.Cell().Text(row.SecondaryLabel);
                                    table.Cell().Text(row.Quantity.ToString("N2"));
                                    table.Cell().Text(row.Amount.ToString("C"));
                                }
                            });
                        });
                    }
                });
            });
        });

        return new FileExportResult
        {
            FileName = $"pos-reports-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            ContentType = "application/pdf",
            Content = document.GeneratePdf()
        };
    }
}

public class SettingsService(IRepository<AppSetting> settingRepository) : ISettingsService
{
    public async Task<List<AppSetting>> GetSettingsAsync() =>
        (await settingRepository.GetAllAsync()).OrderBy(x => x.Key).ToList();
}
