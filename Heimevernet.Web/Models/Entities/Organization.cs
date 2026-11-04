namespace Heimevernet.Web.Models.Entities;

public class Organization
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
}
