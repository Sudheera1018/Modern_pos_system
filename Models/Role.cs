namespace ModernPosSystem.Models;

[BsonCollection("Roles")]
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
