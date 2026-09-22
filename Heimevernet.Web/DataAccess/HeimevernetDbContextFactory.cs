using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Heimevernet.Web.DataAccess;

// EF commands use this factory instead of starting the website through Program.cs.
public class HeimevernetDbContextFactory : IDesignTimeDbContextFactory<HeimevernetDbContext>
{
    public HeimevernetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__heimevernetdb")
            ?? "Server=localhost;Database=heimevernetdb;User=root;Password=;";

        // A fixed compatibility version lets migrations be generated without a running database.
        var options = new DbContextOptionsBuilder<HeimevernetDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("10.11.0-mariadb"))
            .Options;

        return new HeimevernetDbContext(options);
    }
}
