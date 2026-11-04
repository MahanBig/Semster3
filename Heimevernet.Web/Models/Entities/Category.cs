namespace Heimevernet.Web.Models.Entities;

// Farge og ikon ligger i ResourceCategory.cs, slått opp på Name.
public class Category
{
    public int Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
