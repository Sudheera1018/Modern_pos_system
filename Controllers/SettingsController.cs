using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Settings)]
public class SettingsController(ISettingsService settingsService) : Controller
{
    public async Task<IActionResult> Index() => View(await settingsService.GetSettingsAsync());
}
