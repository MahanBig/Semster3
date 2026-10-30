namespace Heimevernet.Web.Models.Entities;

// Én rad i tabellen Positions: et punkt en bruker har registrert på kartet.
public class Position
{
    public int Id { get; init; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
