namespace ModernPosSystem.Models;

[BsonCollection("Forms")]
public class AppForm : BaseEntity
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string Route { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
