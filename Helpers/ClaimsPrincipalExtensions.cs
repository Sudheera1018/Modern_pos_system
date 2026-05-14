using System.Security.Claims;

namespace ModernPosSystem.Helpers;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetUsername(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.Name);

    public static string? GetFullName(this ClaimsPrincipal principal) => principal.FindFirstValue("FullName");

    public static string? GetRoleId(this ClaimsPrincipal principal) => principal.FindFirstValue("RoleId");

    public static string? GetRoleName(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.Role);
}
