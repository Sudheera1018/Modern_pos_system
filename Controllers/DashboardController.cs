using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Dashboard)]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index() => View(await dashboardService.GetDashboardAsync());
}
