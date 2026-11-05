using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Henter og lagrer ressurser i MariaDB med Dapper.
// Vi skriver SQL-en selv. Dapper kjører den og fyller Resource-objekter: kolonnenavn = egenskapsnavn.
public class DapperResourceRepository(IDbConnectionFactory connectionFactory) : IResourceRepository
{
    // LEFT JOIN fordi adresse og eier kan mangle.
    // Latitude/Longitude er DECIMAL i databasen. CAST gjør dem til DOUBLE, som passer double? i C#.
    private const string SelectResources =
        """
        SELECT r.Id, r.Name, r.Description, r.Quantity, r.Status, r.RegisteredAt, r.UserId, r.CategoryId, r.LocationId,
               c.Name AS CategoryName,
               l.Address, l.PostalCode, p.PostalPlace,
               CAST(l.Latitude AS DOUBLE) AS Latitude, CAST(l.Longitude AS DOUBLE) AS Longitude,
               u.Name AS OwnerName, u.Email AS OwnerEmail, u.PhoneNumber AS OwnerPhone
        FROM Resources r
        INNER JOIN Categories c ON c.Id = r.CategoryId
        LEFT JOIN Locations l ON l.Id = r.LocationId
        LEFT JOIN PostalCodes p ON p.PostalCode = l.PostalCode
        LEFT JOIN AspNetUsers u ON u.Id = r.UserId
        """;

    public IReadOnlyCollection<Resource> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Resource>($"{SelectResources} ORDER BY r.Id").ToList().AsReadOnly();
    }

    public Resource? GetById(int id) // Returnerer null hvis det ikke finnes en ressurs med denne id-en.
    {
        using var connection = connectionFactory.CreateConnection();

        // @Id er en parameter. Verdien sendes separat fra SQL-teksten, så ingen kan endre spørringen
        // ved å sende inn ondsinnet tekst (SQL injection). Lim aldri brukerdata rett inn i SQL-strengen.
        return connection.QuerySingleOrDefault<Resource>($"{SelectResources} WHERE r.Id = @Id", new { Id = id });
    }

    public int Add(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        // Transaksjon: lokasjonen og ressursen lagres sammen eller ikke i det hele tatt.
        using var transaction = connection.BeginTransaction();

        resource.LocationId = LocationWriter.Save(
            connection, transaction, existingLocationId: null, resource.Address, resource.PostalCode, resource.PostalPlace,
            resource.Latitude, resource.Longitude);

        // Id står ikke med: MariaDB lager den (AUTO_INCREMENT), og RETURNING Id gir oss den tilbake.
        var id = connection.ExecuteScalar<int>(
            """
            INSERT INTO Resources (Name, Description, Quantity, Status, RegisteredAt, UserId, CategoryId, LocationId)
            VALUES (@Name, @Description, @Quantity, @Status, @RegisteredAt, @UserId, @CategoryId, @LocationId)
            RETURNING Id
            """,
            resource,
            transaction);

        transaction.Commit();
        return id;
    }

    public bool Update(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var oldLocationId = connection.QuerySingleOrDefault<int?>(
            "SELECT LocationId FROM Resources WHERE Id = @Id", new { resource.Id }, transaction);

        resource.LocationId = LocationWriter.Save(
            connection, transaction, oldLocationId, resource.Address, resource.PostalCode, resource.PostalPlace,
            resource.Latitude, resource.Longitude);

        // UserId og RegisteredAt endres ikke.
        var changedRows = connection.Execute(
            """
            UPDATE Resources
            SET Name = @Name, Description = @Description, Quantity = @Quantity, Status = @Status,
                CategoryId = @CategoryId, LocationId = @LocationId
            WHERE Id = @Id
            """,
            resource,
            transaction);

        if (resource.LocationId is null)
        {
            LocationWriter.DeleteIfUnused(connection, transaction, oldLocationId);
        }

        transaction.Commit();
        return changedRows == 1;
    }

    public bool Delete(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var locationId = connection.QuerySingleOrDefault<int?>(
            "SELECT LocationId FROM Resources WHERE Id = @Id", new { Id = id }, transaction);

        // Tildelingene slettes av ON DELETE CASCADE.
        var deleted = connection.Execute("DELETE FROM Resources WHERE Id = @Id", new { Id = id }, transaction) == 1;
        LocationWriter.DeleteIfUnused(connection, transaction, locationId);

        transaction.Commit();
        return deleted;
    }
}
