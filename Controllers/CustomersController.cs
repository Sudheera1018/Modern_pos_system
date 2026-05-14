using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Customers)]
public class CustomersController(
    ICustomerService customerService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index(string? searchTerm)
    {
        ViewBag.SearchTerm = searchTerm;
        return View(await customerService.GetListAsync(searchTerm));
    }

    public async Task<IActionResult> History(string id)
    {
        ViewBag.Customer = await customerService.GetFormAsync(id);
        return View(await customerService.GetPurchaseHistoryAsync(id));
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View("Form", await customerService.GetFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerFormViewModel model) => await Save(model);

    [HttpGet]
    public async Task<IActionResult> Edit(string id) => View("Form", await customerService.GetFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerFormViewModel model) => await Save(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await customerService.DeleteAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(CustomerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors.");
            return View("Form", model);
        }

        var result = await customerService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
