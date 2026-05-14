using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using ModernPosSystem.Helpers;
using ModernPosSystem.Models;
using ModernPosSystem.Repositories;
using ModernPosSystem.ViewModels;

namespace ModernPosSystem.Services;

public class AuthService(
    IRepository<SystemUser> userRepository,
    IPasswordHasher<SystemUser> passwordHasher) : IAuthService
{
    public async Task<ServiceResult<SystemUser>> AuthenticateAsync(string username, string password)
    {
        var user = await userRepository.GetFirstOrDefaultAsync(x => x.Username.ToLower() == username.ToLower() && x.IsActive);
        if (user is null)
        {
            return ServiceResult<SystemUser>.Failure("Invalid username or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return ServiceResult<SystemUser>.Failure("Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = user.Username;
        await userRepository.UpdateAsync(user);

        return ServiceResult<SystemUser>.Success(user);
    }

    public async Task SignInAsync(HttpContext context, SystemUser user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Username),
            new("FullName", user.FullName),
            new("RoleId", user.RoleId),
            new(ClaimTypes.Role, user.RoleName),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    public async Task SignOutAsync(HttpContext context) =>
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}

public class PermissionService(
    IRepository<Role> roleRepository,
    IRepository<AppForm> formRepository,
    IRepository<RoleFormPermission> permissionRepository) : IPermissionService
{
    public async Task<bool> HasAccessAsync(string roleId, string formKey)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return false;
        }

        var permission = await permissionRepository.GetFirstOrDefaultAsync(x => x.RoleId == roleId && x.FormKey == formKey && x.IsActive);
        return permission?.CanAccess ?? false;
    }

    public async Task<List<SidebarMenuItemViewModel>> GetSidebarMenuAsync(string roleId, string currentPath)
    {
        var forms = await formRepository.GetAllAsync(x => x.IsActive);
        var permissions = await permissionRepository.GetAllAsync(x => x.RoleId == roleId && x.CanAccess && x.IsActive);
        var allowedKeys = permissions.Select(x => x.FormKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return forms
            .Where(x => allowedKeys.Contains(x.Key))
            .OrderBy(x => x.SortOrder)
            .Select(x => new SidebarMenuItemViewModel
            {
                Key = x.Key,
                Name = x.Name,
                Icon = x.Icon,
                Route = x.Route,
                IsActive = currentPath.StartsWith(x.Route, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
    }

    public async Task<List<LookupOptionViewModel>> GetRoleOptionsAsync() =>
        (await roleRepository.GetAllAsync(x => x.IsActive))
        .OrderBy(x => x.Name)
        .Select(x => new LookupOptionViewModel { Id = x.Id, Name = x.Name })
        .ToList();

    public async Task<List<RoleFormViewModel>> GetRolesAsync() =>
        (await roleRepository.GetAllAsync())
        .OrderBy(x => x.Name)
        .Select(x => new RoleFormViewModel
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsActive = x.IsActive
        })
        .ToList();

    public async Task<RoleFormViewModel> GetRoleFormAsync(string? id = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return new RoleFormViewModel();
        }

        var role = await roleRepository.GetByIdAsync(id);
        if (role is null)
        {
            return new RoleFormViewModel();
        }

        return new RoleFormViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive
        };
    }

    public async Task<ServiceResult> SaveRoleAsync(RoleFormViewModel model, string userName)
    {
        var duplicate = await roleRepository.GetFirstOrDefaultAsync(x =>
            x.Name.ToLower() == model.Name.ToLower() &&
            x.Id != (model.Id ?? string.Empty));

        if (duplicate is not null)
        {
            return ServiceResult.Failure("A role with the same name already exists.");
        }

        if (string.IsNullOrWhiteSpace(model.Id))
        {
            await roleRepository.AddAsync(new Role
            {
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                IsActive = model.IsActive,
                CreatedBy = userName
            });

            return ServiceResult.Success("Role created successfully.");
        }

        var existing = await roleRepository.GetByIdAsync(model.Id);
        if (existing is null)
        {
            return ServiceResult.Failure("Role not found.");
        }

        existing.Name = model.Name.Trim();
        existing.Description = model.Description.Trim();
        existing.IsActive = model.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = userName;
        await roleRepository.UpdateAsync(existing);

        return ServiceResult.Success("Role updated successfully.");
    }

    public async Task<ServiceResult> ToggleRoleStatusAsync(string id, string userName)
    {
        var role = await roleRepository.GetByIdAsync(id);
        if (role is null)
        {
            return ServiceResult.Failure("Role not found.");
        }

        role.IsActive = !role.IsActive;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = userName;
        await roleRepository.UpdateAsync(role);
        return ServiceResult.Success("Role status updated.");
    }

    public async Task<RolePermissionMatrixViewModel> GetPermissionMatrixAsync(string? roleId)
    {
        var roles = await GetRoleOptionsAsync();
        roleId ??= roles.FirstOrDefault()?.Id ?? string.Empty;
        var role = string.IsNullOrWhiteSpace(roleId) ? null : await roleRepository.GetByIdAsync(roleId);
        var forms = (await formRepository.GetAllAsync(x => x.IsActive)).OrderBy(x => x.SortOrder).ToList();
        var permissions = string.IsNullOrWhiteSpace(roleId) ? [] : await permissionRepository.GetAllAsync(x => x.RoleId == roleId);

        return new RolePermissionMatrixViewModel
        {
            RoleId = roleId,
            RoleName = role?.Name ?? string.Empty,
            Roles = roles,
            Permissions = forms.Select(form => new RolePermissionRowViewModel
            {
                FormKey = form.Key,
                FormName = form.Name,
                Icon = form.Icon,
                CanAccess = permissions.FirstOrDefault(x => x.FormKey == form.Key)?.CanAccess ?? false
            }).ToList()
        };
    }

    public async Task<ServiceResult> SavePermissionMatrixAsync(RolePermissionMatrixViewModel model, string userName)
    {
        var existingPermissions = await permissionRepository.GetAllAsync(x => x.RoleId == model.RoleId);

        foreach (var row in model.Permissions)
        {
            var permission = existingPermissions.FirstOrDefault(x => x.FormKey == row.FormKey);
            if (permission is null)
            {
                await permissionRepository.AddAsync(new RoleFormPermission
                {
                    RoleId = model.RoleId,
                    FormKey = row.FormKey,
                    CanAccess = row.CanAccess,
                    CreatedBy = userName
                });
                continue;
            }

            permission.CanAccess = row.CanAccess;
            permission.IsActive = true;
            permission.UpdatedAt = DateTime.UtcNow;
            permission.UpdatedBy = userName;
            await permissionRepository.UpdateAsync(permission);
        }

        return ServiceResult.Success("Permissions updated successfully.");
    }
}
