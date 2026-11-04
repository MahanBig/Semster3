namespace Heimevernet.Web.DataAccess.Identity;

// Godkjenning av roller (VerifiedBy og VerifiedAt i AspNetUserRoles). Identity kjenner ikke disse kolonnene.
public interface IRoleVerificationRepository
{
    // Tar med roller som venter på godkjenning.
    Task<IReadOnlyList<UserRoleStatus>> GetRolesAsync(string userId);

    // verifiedBy er null når oppstarten godkjenner.
    Task<bool> VerifyAsync(string userId, string roleName, string? verifiedBy);
}

public sealed class UserRoleStatus
{
    public string RoleName { get; init; } = string.Empty;
    public bool RequiresVerification { get; init; }
    public DateTime? VerifiedAt { get; init; } // UTC. Null = ikke godkjent.
    public string? VerifiedByEmail { get; init; }

    // Samme regel som i DapperUserStore.
    public bool IsActive => !RequiresVerification || VerifiedAt is not null;
}
