using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[AllowAnonymous]
public class AccountController(IAuthService authService) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Pos");
        }

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData.PutToast(ToastTypes.Error, "Please enter your username and password.");
            return View(model);
        }

        var result = await authService.AuthenticateAsync(model.Username, model.Password);
        if (!result.Succeeded || result.Data is null)
        {
            TempData.PutToast(ToastTypes.Error, result.Message);
            return View(model);
        }

        await authService.SignInAsync(HttpContext, result.Data, model.RememberMe);
        TempData.PutToast(ToastTypes.Success, $"Welcome back, {result.Data.FullName}.");
        return RedirectToAction("Index", "Pos");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await authService.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }

    public IActionResult AccessDenied() => View();
}
