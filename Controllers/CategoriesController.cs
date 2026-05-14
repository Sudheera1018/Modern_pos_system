using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Categories)]
public class CategoriesController(
    ICategoryService categoryService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index() => View(await categoryService.GetListAsync());

    [HttpGet]
    public async Task<IActionResult> Create() => View("Form", await categoryService.GetFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel model) => await Save(model);

    [HttpGet]
    public async Task<IActionResult> Edit(string id) => View("Form", await categoryService.GetFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel model) => await Save(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await categoryService.DeleteAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors.");
            return View("Form", model);
        }

        var result = await categoryService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
