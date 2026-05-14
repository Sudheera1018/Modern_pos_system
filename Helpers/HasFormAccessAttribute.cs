using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ModernPosSystem.Services;

namespace ModernPosSystem.Helpers;

public class HasFormAccessAttribute : TypeFilterAttribute
{
    public HasFormAccessAttribute(string formKey) : base(typeof(FormAccessFilter))
    {
        Arguments = [formKey];
    }
}

public class FormAccessFilter(
    string formKey,
    IPermissionService permissionService,
    ICurrentUserContext currentUser) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!currentUser.IsAuthenticated)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        if (string.IsNullOrWhiteSpace(formKey))
        {
            return;
        }

        var hasAccess = await permissionService.HasAccessAsync(currentUser.RoleId, formKey);
        if (!hasAccess)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
