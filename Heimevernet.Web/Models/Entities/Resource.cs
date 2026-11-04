namespace Heimevernet.Web.Models.Entities;

public class Resource
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Status { get; set; } = ResourceStatus.Available;
    public DateTime RegisteredAt { get; set; }
    public string? UserId { get; set; } // Eieren. Null for eksempeldata.
    public int CategoryId { get; set; }
    public int? LocationId { get; set; }

    // Fra andre tabeller (JOIN)
    public string CategoryName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? PostalPlace { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }
    public string? OwnerPhone { get; set; }
}
