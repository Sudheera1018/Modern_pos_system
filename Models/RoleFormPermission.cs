namespace ModernPosSystem.Models;

[BsonCollection("RoleFormPermissions")]
public class RoleFormPermission : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;

    public string FormKey { get; set; } = string.Empty;

    public bool CanAccess { get; set; }
}
