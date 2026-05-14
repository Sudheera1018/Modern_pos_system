using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.PosBilling)]
public class PosController(
    IPosService posService,
    IProductService productService,
    ICurrentUserContext currentUser) : Controller
{
    public async Task<IActionResult> Index() => View(await posService.GetPageAsync(currentUser));

    [HttpGet]
    public async Task<IActionResult> Search(string? query)
    {
        var products = await productService.SearchForPosAsync(query);
        return Json(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { succeeded = false, message = "Checkout data is invalid." });
        }

        var result = await posService.CompleteCheckoutAsync(model, currentUser);
        if (!result.Succeeded)
        {
            return BadRequest(new { succeeded = false, message = result.Message });
        }

        return Json(new
        {
            succeeded = true,
            message = result.Message,
            invoiceId = result.Data,
            redirectUrl = Url.Action("Invoice", "SalesHistory", new { id = result.Data })
        });
    }
}
