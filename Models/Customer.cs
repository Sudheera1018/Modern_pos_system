namespace ModernPosSystem.Models;

[BsonCollection("Customers")]
public class Customer : BaseEntity
{
    public string CustomerCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public decimal LoyaltyPoints { get; set; }
}
