using Dapper;
using Heimevernet.Web.Models;

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

        // Eksempeldataene har ingen eier, så bare admin kan endre dem.
        var sampleResources = new[]
        {
            new { Name = "Eksempel: Nødstrømsaggregat", Description = "Bensindrevet aggregat som kan lånes ut.", Category = "Strøm og lading", Quantity = 1 },
            new { Name = "Eksempel: Drikkevann på kanne", Description = "10-liters kanner med rent drikkevann.", Category = "Vann", Quantity = 20 },
            new { Name = "Eksempel: Førstehjelpsskrin", Description = "Skrin med bandasjer, plaster og teip.", Category = "Helse", Quantity = 5 }
        };

        // Når Dapper får en liste, kjøres INSERT én gang per element.
        // Id står ikke med: MariaDB lager den selv (AUTO_INCREMENT).
        connection.Execute(
            """
            INSERT INTO Resources (Name, Description, Quantity, Status, RegisteredAt, CategoryId)
            SELECT @Name, @Description, @Quantity, @Status, @RegisteredAt, Id
            FROM Categories
            WHERE Name = @Category
            """,
            sampleResources.Select(resource => new
            {
                resource.Name,
                resource.Description,
                resource.Quantity,
                resource.Category,
                Status = ResourceStatus.Available,
                RegisteredAt = DateTime.UtcNow
            }));
    }
}
