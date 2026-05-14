using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ModernPosSystem.Models;

public abstract class BaseEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = "system";

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; } = true;
}
