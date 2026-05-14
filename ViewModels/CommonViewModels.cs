using Microsoft.AspNetCore.Mvc.Rendering;

namespace ModernPosSystem.ViewModels;

public class SidebarMenuItemViewModel
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class SummaryCardViewModel
{
    public string Title { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string AccentClass { get; set; } = string.Empty;
}

public class LookupOptionViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class FilterBarViewModel
{
    public string SearchTerm { get; set; } = string.Empty;

    public string? Status { get; set; }

    public IEnumerable<SelectListItem> Options { get; set; } = [];
}
