using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Lagrer kartposisjoner i tabellen Positions med Dapper.
public class DapperPositionRepository(IDbConnectionFactory connectionFactory) : IPositionRepository
{
    public IReadOnlyCollection<Position> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Position>(
                "SELECT Id, Latitude, Longitude, Description, CreatedBy, CreatedAtUtc FROM Positions ORDER BY Id")
            .ToList()
            .AsReadOnly();
    }

    public int Add(Position position)
    {
        ArgumentNullException.ThrowIfNull(position);

        using var connection = connectionFactory.CreateConnection();

        // RETURNING Id gir oss Id-en MariaDB lagde (AUTO_INCREMENT) i samme spørring.
        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Positions (Latitude, Longitude, Description, CreatedBy, CreatedAtUtc)
            VALUES (@Latitude, @Longitude, @Description, @CreatedBy, @CreatedAtUtc)
            RETURNING Id
            """,
            position);
    }
}
