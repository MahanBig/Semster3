namespace Heimevernet.Web.Models.Entities;

public class Allocation
{
    public int NeedId { get; init; }
    public int ResourceId { get; init; }
    public int Quantity { get; set; } = 1;
    public DateTime AllocatedAt { get; set; } // UTC

    // Fra andre tabeller (JOIN)
    public string ResourceName { get; set; } = string.Empty;
    public string? ResourceOwnerId { get; set; }
    public string? ResourceOwnerName { get; set; }
}
