using Heimevernet.Web.Models.ViewModels.Need;
using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Models.ViewModels.UserAdmin;

// Alt admin-siden viser: brukerne med rollen sin, alle ressursene og alle behovene.
public class AdminPageViewModel
{
    public IReadOnlyList<AdminUserRow> Users { get; init; } = [];
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];
    public IReadOnlyList<NeedViewModel> Needs { get; init; } = [];
    public string? CurrentUserId { get; init; } // Admin-en som ser siden. Hen kan ikke slette seg selv eller miste admin.
}

// Én rad i brukertabellen.
public class AdminUserRow
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Role { get; init; } // Den aktive rollen (Admin, Offentlig eller Privat). Null = ingen.
    public int ResourceCount { get; init; }
}

// Det skjemaene for rolle og sletting (_UserRoleForm, _UserDeleteForm) trenger.
// Skjemaene vises to steder (tabellen på PC og kortene på mobil). IdPrefix gjør at id-ene blir unike på siden,
// ellers ville to <select> fått samme id, og <label for="..."> ville pekt på feil felt.
public record AdminUserActions(AdminUserRow User, bool IsMe, string IdPrefix);
