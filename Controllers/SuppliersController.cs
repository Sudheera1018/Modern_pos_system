using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Suppliers)]
public class SuppliersController(
    ISupplierService supplierService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index(string? searchTerm)
    {
        ViewBag.SearchTerm = searchTerm;
        return View(await supplierService.GetListAsync(searchTerm));
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View("Form", await supplierService.GetFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model) => await Save(model);

    [HttpGet]
    public async Task<IActionResult> Edit(string id) => View("Form", await supplierService.GetFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierFormViewModel model) => await Save(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await supplierService.DeleteAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors.");
            return View("Form", model);
        }

        var result = await supplierService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
