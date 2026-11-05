namespace Heimevernet.Web.Models.Entities;

// Én rad i Allocations: en ressurs som er tildelt et behov, og hvor mange.
public class Allocation
{
    public int NeedId { get; init; }
    public int ResourceId { get; init; }
    public int Quantity { get; set; } = 1;
    public DateTime AllocatedAt { get; set; } // UTC

    // Fra andre tabeller (JOIN)
    public string ResourceName { get; set; } = string.Empty;
    public string? ResourceOwnerId { get; set; } // Brukes til å sjekke hvem som kan fjerne tildelingen.
    public string? ResourceOwnerName { get; set; }
}
