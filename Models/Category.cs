namespace ModernPosSystem.Models;

[BsonCollection("Categories")]
public class Category : BaseEntity
{
    public string CategoryId { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
