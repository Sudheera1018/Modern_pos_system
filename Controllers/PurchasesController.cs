using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Purchases)]
public class PurchasesController(
    IPurchaseService purchaseService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index() => View(await purchaseService.GetPageAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PurchaseFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.PutToast(ToastTypes.Error, "Please complete the GRN details.");
            return View("Index", await purchaseService.GetPageAsync());
        }

        var result = await purchaseService.SaveAsync(model, currentUser);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(string id)
    {
        var detail = await purchaseService.GetDetailsAsync(id);
        if (detail is null)
        {
            TempData.PutToast(ToastTypes.Error, "GRN record was not found.");
            return RedirectToAction(nameof(Index));
        }

        return View(detail);
    }
}
