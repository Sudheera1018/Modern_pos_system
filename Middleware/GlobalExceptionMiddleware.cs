using System.Net;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using ModernPosSystem.Helpers;

namespace ModernPosSystem.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for request {Path}", context.Request.Path);

            if (context.Request.Headers.XRequestedWith == "XMLHttpRequest" ||
                context.Request.Path.StartsWithSegments("/Pos"))
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                await context.Response.WriteAsJsonAsync(new { succeeded = false, message = "An unexpected error occurred." });
                return;
            }

            if (context.RequestServices.GetService<ITempDataDictionaryFactory>() is { } tempDataFactory)
            {
                var tempData = tempDataFactory.GetTempData(context);
                tempData.PutToast(ToastTypes.Error, "An unexpected error occurred. Please try again.");
            }

            context.Response.Redirect("/Home/Error");
        }
    }
}
