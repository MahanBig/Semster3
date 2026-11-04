namespace Heimevernet.Web.Models.Entities;

public class Need
{
    public int Id { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Priority { get; set; } = NeedPriority.Medium;
    public string Status { get; set; } = NeedStatus.Open;
    public DateTime RegisteredAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string? UserId { get; set; }
    public int? OrganizationId { get; set; }
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }

    // Fra andre tabeller (JOIN)
    public string CategoryName { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? PostalPlace { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public string? OwnerPhone { get; set; }
    public int AllocatedQuantity { get; set; }
}
