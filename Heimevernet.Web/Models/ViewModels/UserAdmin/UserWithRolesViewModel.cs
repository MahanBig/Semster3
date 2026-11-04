using Heimevernet.Web.DataAccess.Identity;

namespace Heimevernet.Web.Models.ViewModels.UserAdmin;

// Én rad i brukeroversikten: brukeren og rollene hen har.
public class UserWithRolesViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<UserRoleStatus> Roles { get; set; } = [];
}
