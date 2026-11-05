namespace Heimevernet.Web.Models;

// Data til feilsiden. RequestId gjør det mulig å finne den samme feilen igjen i loggen.
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
