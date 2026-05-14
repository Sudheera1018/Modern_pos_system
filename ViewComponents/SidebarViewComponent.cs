using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;

namespace ModernPosSystem.ViewComponents;

public class SidebarViewComponent(
    IPermissionService permissionService,
    ICurrentUserContext currentUser) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (!currentUser.IsAuthenticated)
        {
            return View(new List<ViewModels.SidebarMenuItemViewModel>());
        }

        var currentPath = HttpContext.Request.Path.ToString();
        var menuItems = await permissionService.GetSidebarMenuAsync(currentUser.RoleId, currentPath);
        return View(menuItems);
    }
}
