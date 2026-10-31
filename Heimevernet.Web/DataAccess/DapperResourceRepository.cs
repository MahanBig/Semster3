using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Henter ressurser fra MariaDB med Dapper.
// Vi skriver SQL-en selv. Dapper kjører den og fyller Resource-objekter: kolonnenavn = egenskapsnavn.
public class DapperResourceRepository(IDbConnectionFactory connectionFactory) : IResourceRepository
{
    private const string SelectResources =
        "SELECT Id, Name, Description, Type, Address, City, ZipCode, OwnerTelephoneNumber FROM Resources";

    public IReadOnlyCollection<Resource> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Resource>($"{SelectResources} ORDER BY Id").ToList().AsReadOnly();
    }

    public Resource? GetById(int id) // Returnerer null hvis det ikke finnes en ressurs med denne id-en.
    {
        using var connection = connectionFactory.CreateConnection();

        // @Id er en parameter. Verdien sendes separat fra SQL-teksten, så ingen kan endre spørringen
        // ved å sende inn ondsinnet tekst (SQL injection). Lim aldri brukerdata rett inn i SQL-strengen.
        return connection.QuerySingleOrDefault<Resource>($"{SelectResources} WHERE Id = @Id", new { Id = id });
    }

    public int Add(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        using var connection = connectionFactory.CreateConnection();

        // Id står ikke med: MariaDB lager den (AUTO_INCREMENT), og RETURNING Id gir oss den tilbake.
        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Resources (Name, Description, Type, Address, City, ZipCode, OwnerTelephoneNumber)
            VALUES (@Name, @Description, @Type, @Address, @City, @ZipCode, @OwnerTelephoneNumber)
            RETURNING Id
            """,
            resource);
    }
}
