namespace Heimevernet.Web.Models.Entities;

// Bildet til én ressurs. Tilsvarer én rad i ResourceImages.
public class ResourceImage
{
    public int ResourceId { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public byte[] Data { get; init; } = [];
    public DateTime UploadedAt { get; init; } // UTC
}
