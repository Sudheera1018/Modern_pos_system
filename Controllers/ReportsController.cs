using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Reports)]
public class ReportsController(
    IReportService reportService,
    IUserService userService,
    IProductService productService) : Controller
{
    public async Task<IActionResult> Index(ReportFilterViewModel filter)
    {
        var dto = new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            CashierUserId = filter.CashierUserId,
            ProductId = filter.ProductId
        };

        var model = await reportService.GetReportsAsync(dto);
        model.Filter = filter;
        model.Filter.Cashiers = await userService.GetCashierLookupAsync();
        model.Filter.Products = await productService.GetLookupAsync();
        return View(model);
    }

    public async Task<FileResult> ExportExcel(ReportFilterViewModel filter)
    {
        var export = await reportService.ExportExcelAsync(new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            CashierUserId = filter.CashierUserId,
            ProductId = filter.ProductId
        });
        return File(export.Content, export.ContentType, export.FileName);
    }

    public async Task<FileResult> ExportPdf(ReportFilterViewModel filter)
    {
        var export = await reportService.ExportPdfAsync(new ReportFilterDto
        {
            FromDate = filter.FromDate,
            ToDate = filter.ToDate,
            CashierUserId = filter.CashierUserId,
            ProductId = filter.ProductId
        });
        return File(export.Content, export.ContentType, export.FileName);
    }
}
