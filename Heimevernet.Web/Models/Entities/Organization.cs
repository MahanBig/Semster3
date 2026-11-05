namespace Heimevernet.Web.Models.Entities;

// Én rad i Organizations. Et behov kan høre til en organisasjon.
public class Organization
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
}
