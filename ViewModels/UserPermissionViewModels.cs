using System.ComponentModel.DataAnnotations;

namespace ModernPosSystem.ViewModels;

public class UserFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string RoleId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<LookupOptionViewModel> Roles { get; set; } = [];
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class RoleFormViewModel
{
    public string? Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class RolePermissionMatrixViewModel
{
    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public List<RolePermissionRowViewModel> Permissions { get; set; } = [];

    public List<LookupOptionViewModel> Roles { get; set; } = [];
}

public class RolePermissionRowViewModel
{
    public string FormKey { get; set; } = string.Empty;

    public string FormName { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public bool CanAccess { get; set; }
}
