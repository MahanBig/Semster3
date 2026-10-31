using Dapper;
using Microsoft.AspNetCore.Identity;

namespace Heimevernet.Web.DataAccess.Identity;

// UserManager og SignInManager (fra Identity) bruker denne klassen til å lese og lagre brukere.
// Hvert interface er en "evne" Identity kan bruke:
//   IUserPasswordStore      -> passord-hash (innlogging)
//   IUserEmailStore         -> e-post (FindByEmailAsync, unik e-post)
//   IUserRoleStore          -> roller (AddToRoleAsync, IsInRoleAsync, [Authorize(Roles = ...)])
//   IUserSecurityStampStore -> gjør gamle innlogginger ugyldige når passordet endres
//   IUserLockoutStore       -> låser kontoen etter for mange feil passord
//   IQueryableUserStore     -> userManager.Users (listen på brukeradmin-siden)
public class DapperUserStore(IDbConnectionFactory connectionFactory, IdentityErrorDescriber errorDescriber) :
    IUserPasswordStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser>,
    IUserLockoutStore<IdentityUser>,
    IQueryableUserStore<IdentityUser>
{
    private const string SelectUsers = """
        SELECT Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash,
               SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
               LockoutEnd, LockoutEnabled, AccessFailedCount
        FROM AspNetUsers
        """;

    // ---------- Lagre, endre og slette ----------

    public async Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled,
                LockoutEnd, LockoutEnabled, AccessFailedCount)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @EmailConfirmed,
                @PasswordHash, @SecurityStamp, @ConcurrencyStamp, @PhoneNumber, @PhoneNumberConfirmed, @TwoFactorEnabled,
                @LockoutEnd, @LockoutEnabled, @AccessFailedCount)
            """,
            UserRow.FromIdentityUser(user),
            cancellationToken: cancellationToken));
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        // Optimistisk låsing: oppdater bare hvis ingen andre har endret brukeren siden vi leste den.
        // Hvis ConcurrencyStamp i databasen er endret, treffer WHERE ingen rader, og vi gir feil.
        var oldConcurrencyStamp = user.ConcurrencyStamp;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();

        var parameters = new DynamicParameters(UserRow.FromIdentityUser(user));
        parameters.Add("OldConcurrencyStamp", oldConcurrencyStamp);

        using var connection = connectionFactory.CreateConnection();
        var changedRows = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE AspNetUsers
            SET UserName = @UserName, NormalizedUserName = @NormalizedUserName,
                Email = @Email, NormalizedEmail = @NormalizedEmail, EmailConfirmed = @EmailConfirmed,
                PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp, ConcurrencyStamp = @ConcurrencyStamp,
                PhoneNumber = @PhoneNumber, PhoneNumberConfirmed = @PhoneNumberConfirmed,
                TwoFactorEnabled = @TwoFactorEnabled, LockoutEnd = @LockoutEnd,
                LockoutEnabled = @LockoutEnabled, AccessFailedCount = @AccessFailedCount
            WHERE Id = @Id AND ConcurrencyStamp = @OldConcurrencyStamp
            """,
            parameters,
            cancellationToken: cancellationToken));

        return changedRows == 1 ? IdentityResult.Success : IdentityResult.Failed(errorDescriber.ConcurrencyFailure());
    }

    public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM AspNetUsers WHERE Id = @Id", new { user.Id }, cancellationToken: cancellationToken));
        return IdentityResult.Success;
    }

    // ---------- Finne brukere ----------

    public Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => FindUserAsync("Id = @Value", userId, cancellationToken);

    // UserManager sender inn "normaliserte" verdier (STORE BOKSTAVER), så "Ola@Test.no" og "ola@test.no" er samme bruker.
    public Task<IdentityUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => FindUserAsync("NormalizedUserName = @Value", normalizedUserName, cancellationToken);

    public Task<IdentityUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => FindUserAsync("NormalizedEmail = @Value", normalizedEmail, cancellationToken);

    // whereClause kommer alltid fra koden over, aldri fra brukeren. Selve verdien sendes som parameter (@Value).
    private async Task<IdentityUser?> FindUserAsync(string whereClause, string value, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            $"{SelectUsers} WHERE {whereClause}", new { Value = value }, cancellationToken: cancellationToken));
        return row?.ToIdentityUser();
    }

    // userManager.Users. Henter alle brukerne. Greit for en liten app, men med tusenvis av brukere
    // burde vi heller hatt en egen metode med paging (LIMIT/OFFSET).
    public IQueryable<IdentityUser> Users
    {
        get
        {
            using var connection = connectionFactory.CreateConnection();
            return connection.Query<UserRow>(SelectUsers)
                .Select(row => row.ToIdentityUser())
                .ToList()
                .AsQueryable();
        }
    }

    // ---------- Roller (tabellen AspNetUserRoles) ----------
    // roleName er normalisert (STORE BOKSTAVER), derfor sammenligner vi med NormalizedName.

    public async Task AddToRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var roleId = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
                "SELECT Id FROM AspNetRoles WHERE NormalizedName = @NormalizedName",
                new { NormalizedName = roleName },
                cancellationToken: cancellationToken))
            ?? throw new InvalidOperationException($"Role '{roleName}' does not exist.");

        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)",
            new { UserId = user.Id, RoleId = roleId },
            cancellationToken: cancellationToken));
    }

    public async Task RemoveFromRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            DELETE FROM AspNetUserRoles
            WHERE UserId = @UserId
              AND RoleId IN (SELECT Id FROM AspNetRoles WHERE NormalizedName = @NormalizedName)
            """,
            new { UserId = user.Id, NormalizedName = roleName },
            cancellationToken: cancellationToken));
    }

    public async Task<IList<string>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var roleNames = await connection.QueryAsync<string>(new CommandDefinition(
            """
            SELECT r.Name
            FROM AspNetRoles r
            INNER JOIN AspNetUserRoles ur ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId
            ORDER BY r.Name
            """,
            new { UserId = user.Id },
            cancellationToken: cancellationToken));
        return roleNames.ToList();
    }

    public async Task<bool> IsInRoleAsync(IdentityUser user, string roleName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            """
            SELECT COUNT(*)
            FROM AspNetUserRoles ur
            INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
            WHERE ur.UserId = @UserId AND r.NormalizedName = @NormalizedName
            """,
            new { UserId = user.Id, NormalizedName = roleName },
            cancellationToken: cancellationToken));
        return count > 0;
    }

    public async Task<IList<IdentityUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<UserRow>(new CommandDefinition(
            $"""
            {SelectUsers}
            WHERE Id IN (
                SELECT ur.UserId
                FROM AspNetUserRoles ur
                INNER JOIN AspNetRoles r ON r.Id = ur.RoleId
                WHERE r.NormalizedName = @NormalizedName)
            """,
            new { NormalizedName = roleName },
            cancellationToken: cancellationToken));
        return rows.Select(row => row.ToIdentityUser()).ToList();
    }

    // ---------- Get/Set på objektet i minnet ----------
    // Disse rører ikke databasen. UserManager endrer objektet først og kaller så UpdateAsync for å lagre alt.

    public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedUserName);

    public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash);

    public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.PasswordHash is not null);

    public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Email);

    public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.EmailConfirmed);

    public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.NormalizedEmail);

    public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    public Task SetSecurityStampAsync(IdentityUser user, string stamp, CancellationToken cancellationToken)
    {
        user.SecurityStamp = stamp;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnd);

    public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
    {
        user.LockoutEnd = lockoutEnd;
        return Task.CompletedTask;
    }

    public Task<int> IncrementAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;
        return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        user.AccessFailedCount = 0;
        return Task.CompletedTask;
    }

    public Task<int> GetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.AccessFailedCount);

    public Task<bool> GetLockoutEnabledAsync(IdentityUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.LockoutEnabled);

    public Task SetLockoutEnabledAsync(IdentityUser user, bool enabled, CancellationToken cancellationToken)
    {
        user.LockoutEnabled = enabled;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // Ingenting å rydde: hver metode lukker sin egen tilkobling med "using".
    }
}
