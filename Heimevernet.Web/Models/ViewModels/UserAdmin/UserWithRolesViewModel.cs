namespace Heimevernet.Web.Models.ViewModels.UserAdmin;

// Én rad i brukeroversikten: brukeren og rollene hen har.
public class UserWithRolesViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = [];
}
