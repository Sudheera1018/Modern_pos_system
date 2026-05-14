using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.SalesHistory)]
public class SalesHistoryController(
    ISalesService salesService,
    IUserService userService) : Controller
{
    public async Task<IActionResult> Index(string? invoiceNo, DateTime? fromDate, DateTime? toDate, string? cashierUserId)
    {
        var model = await salesService.GetHistoryAsync(invoiceNo, fromDate, toDate, cashierUserId);
        model.Cashiers = await userService.GetCashierLookupAsync();
        return View(model);
    }

    public async Task<IActionResult> Invoice(string id)
    {
        var invoice = await salesService.GetInvoiceAsync(id);
        if (invoice is null)
        {
            TempData.PutToast(ToastTypes.Error, "Invoice not found.");
            return RedirectToAction(nameof(Index));
        }

        return View(invoice);
    }
}
