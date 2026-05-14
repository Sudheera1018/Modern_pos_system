using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.RolePermissions)]
public class RolePermissionsController(
    IPermissionService permissionService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index(string? roleId) => View(await permissionService.GetPermissionMatrixAsync(roleId));

    [HttpGet]
    public async Task<IActionResult> Roles() => View(await permissionService.GetRolesAsync());

    [HttpGet]
    public async Task<IActionResult> CreateRole() => View("RoleForm", await permissionService.GetRoleFormAsync());

    [HttpGet]
    public async Task<IActionResult> EditRole(string id) => View("RoleForm", await permissionService.GetRoleFormAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRole(RoleFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("RoleForm", model);
        }

        var result = await permissionService.SaveRoleAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        if (!result.Succeeded)
        {
            return View("RoleForm", model);
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleRoleStatus(string id)
    {
        var result = await permissionService.ToggleRoleStatusAsync(id, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(RolePermissionMatrixViewModel model)
    {
        var result = await permissionService.SavePermissionMatrixAsync(model, currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, result.Message);
        return RedirectToAction(nameof(Index), new { roleId = model.RoleId });
    }
}
