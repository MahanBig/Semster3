using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Heimevernet.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.Web.UnitTests;

// Setter opp de samme Identity-tjenestene som Program.cs (med våre Dapper-stores), men mot SQLite.
internal static class IdentityTestServices
{
    public static ServiceProvider Create(SqliteTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDbConnectionFactory>(database);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddUserStore<DapperUserStore>()
            .AddRoleStore<DapperRoleStore>();
        return services.BuildServiceProvider();
    }
}
