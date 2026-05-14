namespace ModernPosSystem.Models;

[BsonCollection("Users")]
public class SystemUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string RoleId { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }
}
