namespace Heimevernet.Web.Models.Entities;

public class Resource
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? OwnerTelephoneNumber { get; set; }
}
