using System.Security.Claims;

namespace ModernPosSystem.Helpers;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    string UserId { get; }
    string UserName { get; }
    string FullName { get; }
    string RoleId { get; }
    string RoleName { get; }
}

public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public string UserId => User?.GetUserId() ?? string.Empty;

    public string UserName => User?.GetUsername() ?? string.Empty;

    public string FullName => User?.GetFullName() ?? string.Empty;

    public string RoleId => User?.GetRoleId() ?? string.Empty;

    public string RoleName => User?.GetRoleName() ?? string.Empty;
}
