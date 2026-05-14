using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Users)]
public class UsersController(
    IUserService userService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index() => View(await userService.GetListAsync());

    [HttpGet]
    public async Task<IActionResult> Create() => View("Form", await userService.GetFormAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel model) => await Save(model);

    [HttpGet]
    public async Task<IActionResult> Edit(string id) => View("Form", await userService.GetFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel model) => await Save(model);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var result = await userService.ToggleStatusAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var result = await userService.ResetPasswordAsync(id, "Reset@123", currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Save(UserFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await userService.GetFormAsync(model.Id);
            TempData.PutToast(ToastTypes.Error, "Please fix the validation errors.");
            return View("Form", model);
        }

        var result = await userService.SaveAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            model = await userService.GetFormAsync(model.Id);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index));
    }
}
