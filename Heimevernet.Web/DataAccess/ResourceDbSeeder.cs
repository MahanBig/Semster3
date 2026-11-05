using Dapper;
using Heimevernet.Web.Models;

namespace Heimevernet.Web.DataAccess;

// Legger inn noen eksempelressurser første gang appen starter, så listen og kartet ikke er tomme.
// Kalles fra Program.cs etter at DatabaseInitializer har laget tabellene.
public static class ResourceDbSeeder
{
    public static void Seed(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        using var connection = connectionFactory.CreateConnection();

        // Finnes det allerede ressurser, gjør vi ingenting. Da beholdes det brukerne har lagt inn
        // og endret når appen starter på nytt.
        if (connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Resources") > 0)
        {
            return;
        }

        // Eksempeldataene har ingen eier, så bare admin kan endre dem.
        // De har en adresse og et punkt i Kristiansand, så de vises på kartet og får en avstand.
        var sampleResources = new[]
        {
            new { Name = "Eksempel: Nødstrømsaggregat", Description = "Bensindrevet aggregat som kan lånes ut.", Category = "Strøm og lading", Quantity = 1,
                  Address = "Markens gate 20", PostalCode = "4611", PostalPlace = "Kristiansand", Latitude = 58.146400, Longitude = 7.995900 },
            new { Name = "Eksempel: Drikkevann på kanne", Description = "10-liters kanner med rent drikkevann.", Category = "Vann", Quantity = 20,
                  Address = "Universitetsveien 25", PostalCode = "4630", PostalPlace = "Kristiansand", Latitude = 58.163900, Longitude = 8.003600 },
            new { Name = "Eksempel: Førstehjelpsskrin", Description = "Skrin med bandasjer, plaster og teip.", Category = "Helse", Quantity = 5,
                  Address = "Egsveien 100", PostalCode = "4615", PostalPlace = "Kristiansand", Latitude = 58.170800, Longitude = 7.972600 }
        };

        // Én ressurs om gangen i en transaksjon: først adressen (Locations), så ressursen som peker på den.
        // Id står ikke med: MariaDB lager den selv (AUTO_INCREMENT).
        // INSERT ... SELECT slår opp kategori-Id-en fra kategorinavnet i samme spørring.
        connection.Open();
        using var transaction = connection.BeginTransaction();
        foreach (var resource in sampleResources)
        {
            var locationId = LocationWriter.Save(
                connection, transaction, existingLocationId: null,
                resource.Address, resource.PostalCode, resource.PostalPlace, resource.Latitude, resource.Longitude);

            connection.Execute(
                """
                INSERT INTO Resources (Name, Description, Quantity, Status, RegisteredAt, CategoryId, LocationId)
                SELECT @Name, @Description, @Quantity, @Status, @RegisteredAt, Id, @LocationId
                FROM Categories
                WHERE Name = @Category
                """,
                new
                {
                    resource.Name,
                    resource.Description,
                    resource.Quantity,
                    resource.Category,
                    Status = ResourceStatus.Available,
                    RegisteredAt = DateTime.UtcNow,
                    LocationId = locationId
                },
                transaction);
        }
        transaction.Commit();
    }
}
