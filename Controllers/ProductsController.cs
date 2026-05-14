using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Products)]
public class ProductsController(
    IProductService productService,
    ICategoryService categoryService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index(string? searchTerm, string? categoryId)
    {
        ViewBag.Categories = await categoryService.GetLookupAsync();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CategoryId = categoryId;
        return View(await productService.GetListAsync(searchTerm, categoryId));
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View("Form", await productService.GetFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await productService.PrepareFormAsync(model);
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors and try again.");
            return View("Form", model);
        }

        var result = await productService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            model = await productService.PrepareFormAsync(model);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id) => View("Form", await productService.GetFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await productService.PrepareFormAsync(model);
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors and try again.");
            return View("Form", model);
        }

        var result = await productService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            model = await productService.PrepareFormAsync(model);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await productService.DeleteAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }
}
