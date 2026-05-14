using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Inventory)]
public class InventoryController(
    IInventoryService inventoryService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index() => View(await inventoryService.GetOverviewAsync());

    [HttpGet]
    public async Task<IActionResult> Adjustment() => View(await inventoryService.GetAdjustmentFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjustment(StockAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await inventoryService.GetAdjustmentFormAsync();
            TempData.PutToast(ToastTypes.Error, "Please correct the stock adjustment details.");
            return View(model);
        }

        var result = await inventoryService.SaveAdjustmentAsync(model, currentUser);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            model = await inventoryService.GetAdjustmentFormAsync();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }
}
