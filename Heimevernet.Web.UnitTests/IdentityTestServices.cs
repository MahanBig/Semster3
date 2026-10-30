using Heimevernet.Web.DataAccess;
using Heimevernet.Web.DataAccess.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Heimevernet.Web.UnitTests;

// Builds the same Identity services as Program.cs (with our Dapper stores), but against SQLite.
internal static class IdentityTestServices
{
    public static ServiceProvider Create(SqliteTestDatabase database)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDbConnectionFactory>(database);
        services.AddIdentityCore<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddUserStore<DapperUserStore>()
            .AddRoleStore<DapperRoleStore>();
        return services.BuildServiceProvider();
    }
}
