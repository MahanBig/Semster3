using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public static class ResourceDbSeeder
{
    public static void Seed(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        using var connection = connectionFactory.CreateConnection();

        // Preserve existing records and edits when the application restarts.
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Resources") > 0)
        {
            return;
        }

        Resource[] sampleResources =
        [
            new Resource { Name = "Resource 1", Description = "Db Description 1", Type = "Type A" },
            new Resource { Name = "Resource 2", Description = "Db Description 2", Type = "Type B" },
            new Resource { Name = "Resource 3", Description = "Db Description 3", Type = "Type C" }
        ];

        // Når Dapper får en liste, kjøres INSERT én gang per element.
        // Id står ikke med: MariaDB lager den selv (AUTO_INCREMENT).
        connection.Execute(
            "INSERT INTO Resources (Name, Description, Type) VALUES (@Name, @Description, @Type)",
            sampleResources);
    }
}
