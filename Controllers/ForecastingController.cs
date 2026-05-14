using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernPosSystem.DTOs;
using ModernPosSystem.Helpers;
using ModernPosSystem.Services;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Controllers;

[Authorize]
[HasFormAccess(FormModules.Forecasting)]
public class ForecastingController(
    IForecastProviderService forecastProviderService,
    IForecastGenerationService forecastGenerationService,
    ICurrentUserContext currentUser) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ForecastFilterViewModel filter)
    {
        var model = await forecastProviderService.GetForecastingAsync(new ForecastQueryDto
        {
            StoreId = filter.StoreId,
            ForecastDate = filter.ForecastDate,
            ProductId = filter.ProductId,
            CategoryId = filter.CategoryId,
            ReorderStatus = filter.ReorderStatus,
            SlowMovingOnly = filter.SlowMovingOnly
        });
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(ForecastFilterViewModel filter)
    {
        var result = await forecastGenerationService.GenerateForecastForAllActiveProductsAsync(MapGenerationRequest(filter), currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, BuildGenerationMessage(result));
        SetGeneratedWarning(result);
        return RedirectToAction(nameof(Index), ToRouteValues(filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(ForecastFilterViewModel filter)
    {
        var result = await forecastGenerationService.GenerateForecastForStoreAsync(MapGenerationRequest(filter), currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, BuildGenerationMessage(result));
        SetGeneratedWarning(result);
        return RedirectToAction(nameof(Index), ToRouteValues(filter));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateProduct(ForecastFilterViewModel filter)
    {
        if (string.IsNullOrWhiteSpace(filter.ProductId))
        {
            TempData.PutToast(ToastTypes.Error, "Select a product to generate a single-product forecast.");
            return RedirectToAction(nameof(Index), ToRouteValues(filter));
        }

        var result = await forecastGenerationService.GenerateForecastForProductAsync(MapGenerationRequest(filter), currentUser.UserName);
        TempData.PutToast(result.Succeeded ? ToastTypes.Success : ToastTypes.Error, BuildGenerationMessage(result));
        SetGeneratedWarning(result);
        return RedirectToAction(nameof(Index), ToRouteValues(filter));
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var detail = await forecastProviderService.GetForecastDetailsAsync(id);
        if (detail is null)
        {
            return NotFound();
        }

        return PartialView("_ForecastDetails", detail);
    }

    private static ForecastGenerationRequestDto MapGenerationRequest(ForecastFilterViewModel filter) => new()
    {
        StoreId = filter.StoreId,
        ForecastDate = filter.ForecastDate,
        ProductId = filter.ProductId,
        CategoryId = filter.CategoryId,
        GenerationSource = ForecastGenerationSources.Manual
    };

    private static object ToRouteValues(ForecastFilterViewModel filter) => new
    {
        filter.StoreId,
        ForecastDate = filter.ForecastDate == default ? DateTime.UtcNow.Date : filter.ForecastDate.Date,
        filter.ProductId,
        filter.CategoryId,
        filter.ReorderStatus,
        filter.SlowMovingOnly
    };

    private static string BuildGenerationMessage(ServiceResult<ForecastGenerationSummaryDto> result)
    {
        if (!result.Succeeded || result.Data is null)
        {
            return result.Message;
        }

        if (result.Data.Warnings.Count == 0)
        {
            return result.Message;
        }

        return $"{result.Message} {string.Join(" ", result.Data.Warnings.Take(3))}";
    }

    private void SetGeneratedWarning(ServiceResult<ForecastGenerationSummaryDto> result)
    {
        if (!result.Succeeded || result.Data is null || result.Data.GeneratedProducts <= 0)
        {
            TempData.Remove("ForecastGeneratedMessage");
            return;
        }

        TempData["ForecastGeneratedMessage"] = $"The forecast job completed and saved {result.Data.GeneratedProducts} product forecast(s). If the table is still empty after reload, clear the product, category, reorder, or slow-moving filters and generate again for the selected date.";
    }
}
