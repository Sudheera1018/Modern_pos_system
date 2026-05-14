using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.Models;

namespace ModernPosSystem.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Pos");
        }

        return RedirectToAction("Login", "Account");
    }

    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
