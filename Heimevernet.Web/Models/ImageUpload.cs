using Microsoft.AspNetCore.Http;

namespace Heimevernet.Web.Models;

// Sjekker et opplastet bilde før det lagres.
// Vi stoler ikke på filnavnet eller typen nettleseren sender (de kan forfalskes). I stedet ser vi på de første
// bytene i filen ("magiske tall"), som er faste for hvert bildeformat. Bare JPEG, PNG og WebP godtas.
// Da kan ingen laste opp f.eks. en HTML-fil med script og få den servert som "bilde".
public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024; // 5 MB
    public const string Accept = "image/jpeg,image/png,image/webp"; // Til accept-attributtet i skjemaet.

    public static ImageUploadResult Read(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return ImageUploadResult.None;
        }
        if (file.Length > MaxBytes)
        {
            return ImageUploadResult.Fail("Bildet kan være på maks 5 MB.");
        }

        using var stream = new MemoryStream();
        file.CopyTo(stream);
        var data = stream.ToArray();

        var contentType = DetectContentType(data);
        return contentType is null
            ? ImageUploadResult.Fail("Bildet må være JPEG, PNG eller WebP.")
            : ImageUploadResult.Ok(data, contentType);
    }

    public static string? DetectContentType(byte[] data)
    {
        if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
        {
            return "image/jpeg";
        }
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return "image/png";
        }
        // WebP: "RIFF" + 4 byte lengde + "WEBP"
        if (data.Length >= 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) && data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return "image/webp";
        }
        return null;
    }
}

public sealed record ImageUploadResult(byte[]? Data, string? ContentType, string? Error)
{
    public static readonly ImageUploadResult None = new(null, null, null); // Ingen fil valgt.
    public static ImageUploadResult Ok(byte[] data, string contentType) => new(data, contentType, null);
    public static ImageUploadResult Fail(string error) => new(null, null, error);

    public bool HasImage => Data is not null;
}
