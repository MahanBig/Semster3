using Dapper;

namespace Heimevernet.Web.DataAccess.Identity;

public class DapperRoleVerificationRepository(IDbConnectionFactory connectionFactory) : IRoleVerificationRepository
{
    public async Task<IReadOnlyList<UserRoleStatus>> GetRolesAsync(string userId)
    {
        using var connection = connectionFactory.CreateConnection();

        var rows = await connection.QueryAsync<UserRoleStatus>(
            """
            SELECT r.Name AS RoleName, r.RequiresVerification, ur.VerifiedAt, verifier.Email AS VerifiedByEmail
            FROM AspNetUserRoles ur
            INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
            LEFT JOIN AspNetUsers verifier ON verifier.Id = ur.VerifiedBy
            WHERE ur.UserId = @UserId
            ORDER BY r.Name
            """,
            new { UserId = userId });
        return rows.ToList();
    }

    public async Task<bool> VerifyAsync(string userId, string roleName, string? verifiedBy)
    {
        using var connection = connectionFactory.CreateConnection();
        var changedRows = await connection.ExecuteAsync(
            """
            UPDATE AspNetUserRoles
            SET VerifiedBy = @VerifiedBy, VerifiedAt = @VerifiedAt
            WHERE UserId = @UserId
              AND RoleId IN (SELECT Id FROM AspNetRoles WHERE Name = @RoleName)
            """,
            new { UserId = userId, RoleName = roleName, VerifiedBy = verifiedBy, VerifiedAt = DateTime.UtcNow });
        return changedRows == 1;
    }
}
