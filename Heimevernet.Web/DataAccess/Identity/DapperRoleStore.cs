using Dapper;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.DataAccess.Identity;

// RoleManager (fra Identity) bruker denne klassen til å lese og lagre roller.
// Identity bestemmer HVA som skal gjøres. Vi bestemmer HVORDAN, med SQL mot tabellen AspNetRoles.
public class DapperRoleStore(IDbConnectionFactory connectionFactory, IdentityErrorDescriber errorDescriber)
    : IQueryableRoleStore<ApplicationRole>
{
    private const string SelectRoles =
        "SELECT Id, Name, NormalizedName, ConcurrencyStamp, Description, AccessLevel, RequiresVerification FROM AspNetRoles";

    // RoleManager.Roles. Henter alle radene. Det går fint fordi det bare finnes noen få roller.
    public IQueryable<ApplicationRole> Roles
    {
        get
        {
            using var connection = connectionFactory.CreateConnection();
            return connection.Query<ApplicationRole>(SelectRoles).ToList().AsQueryable();
        }
    }

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp, Description, AccessLevel, RequiresVerification)
            VALUES (@Id, @Name, @NormalizedName, @ConcurrencyStamp, @Description, @AccessLevel, @RequiresVerification)
            """,
            role,
            cancellationToken: cancellationToken));
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);

        // Optimistisk låsing: oppdater bare hvis ingen andre har endret rollen siden vi leste den.
        var oldConcurrencyStamp = role.ConcurrencyStamp;
        role.ConcurrencyStamp = Guid.NewGuid().ToString();

        using var connection = connectionFactory.CreateConnection();
        var changedRows = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE AspNetRoles
            SET Name = @Name, NormalizedName = @NormalizedName, ConcurrencyStamp = @ConcurrencyStamp,
                Description = @Description, AccessLevel = @AccessLevel, RequiresVerification = @RequiresVerification
            WHERE Id = @Id AND ConcurrencyStamp = @OldConcurrencyStamp
            """,
            new
            {
                role.Id, role.Name, role.NormalizedName, role.ConcurrencyStamp,
                role.Description, role.AccessLevel, role.RequiresVerification,
                OldConcurrencyStamp = oldConcurrencyStamp
            },
            cancellationToken: cancellationToken));

        return changedRows == 1 ? IdentityResult.Success : IdentityResult.Failed(errorDescriber.ConcurrencyFailure());
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(role);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AspNetRoles WHERE Id = @Id", new { role.Id }, cancellationToken: cancellationToken));
        return IdentityResult.Success;
    }

    public async Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationRole>(new CommandDefinition(
            $"{SelectRoles} WHERE Id = @Id", new { Id = roleId }, cancellationToken: cancellationToken));
    }

    // RoleManager sender inn navnet "normalisert" (STORE BOKSTAVER), f.eks. "ADMIN".
    public async Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApplicationRole>(new CommandDefinition(
            $"{SelectRoles} WHERE NormalizedName = @NormalizedName",
            new { NormalizedName = normalizedRoleName },
            cancellationToken: cancellationToken));
    }

    // Get/Set-metodene under endrer bare objektet i minnet. RoleManager kaller UpdateAsync etterpå for å lagre.
    public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Id);

    public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.Name);

    public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        => Task.FromResult(role.NormalizedName);

    public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Ingenting å rydde: hver metode lukker sin egen tilkobling med "using".
    }
}
