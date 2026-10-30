using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.DataAccess.Identity;

// Én rad i tabellen AspNetUsers, slik den ser ut i databasen.
// Hvorfor ikke bruke IdentityUser direkte? IdentityUser.LockoutEnd er en DateTimeOffset (tid + tidssone),
// men MariaDB har bare DATETIME uten tidssone. Her lagrer vi tiden i UTC og gjør om begge veier.
public sealed class UserRow
{
    public string Id { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LockoutEnd { get; set; } // UTC
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }

    public static UserRow FromIdentityUser(IdentityUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        NormalizedUserName = user.NormalizedUserName,
        Email = user.Email,
        NormalizedEmail = user.NormalizedEmail,
        EmailConfirmed = user.EmailConfirmed,
        PasswordHash = user.PasswordHash,
        SecurityStamp = user.SecurityStamp,
        ConcurrencyStamp = user.ConcurrencyStamp,
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        TwoFactorEnabled = user.TwoFactorEnabled,
        LockoutEnd = user.LockoutEnd?.UtcDateTime,
        LockoutEnabled = user.LockoutEnabled,
        AccessFailedCount = user.AccessFailedCount
    };

    public IdentityUser ToIdentityUser() => new()
    {
        Id = Id,
        UserName = UserName,
        NormalizedUserName = NormalizedUserName,
        Email = Email,
        NormalizedEmail = NormalizedEmail,
        EmailConfirmed = EmailConfirmed,
        PasswordHash = PasswordHash,
        SecurityStamp = SecurityStamp,
        ConcurrencyStamp = ConcurrencyStamp,
        PhoneNumber = PhoneNumber,
        PhoneNumberConfirmed = PhoneNumberConfirmed,
        TwoFactorEnabled = TwoFactorEnabled,
        LockoutEnd = LockoutEnd is { } lockoutEnd
            ? new DateTimeOffset(DateTime.SpecifyKind(lockoutEnd, DateTimeKind.Utc))
            : null,
        LockoutEnabled = LockoutEnabled,
        AccessFailedCount = AccessFailedCount
    };
}
