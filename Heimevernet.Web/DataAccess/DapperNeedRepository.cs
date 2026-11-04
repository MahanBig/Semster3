using Dapper;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public class DapperNeedRepository(IDbConnectionFactory connectionFactory) : INeedRepository
{
    // AllocatedQuantity er summen av tildelingene til behovet.
    private const string SelectNeeds =
        """
        SELECT n.Id, n.Title, n.Description, n.Quantity, n.Priority, n.Status, n.RegisteredAt, n.Deadline,
               n.UserId, n.OrganizationId, n.CategoryId, n.LocationId,
               c.Name AS CategoryName,
               o.Name AS OrganizationName,
               l.Address, l.PostalCode, p.PostalPlace,
               u.Name AS OwnerName, u.Email AS OwnerEmail, u.PhoneNumber AS OwnerPhone,
               (SELECT COALESCE(SUM(a.Quantity), 0) FROM Allocations a WHERE a.NeedId = n.Id) AS AllocatedQuantity
        FROM Needs n
        INNER JOIN Categories c ON c.Id = n.CategoryId
        LEFT JOIN Organizations o ON o.Id = n.OrganizationId
        LEFT JOIN Locations l ON l.Id = n.LocationId
        LEFT JOIN PostalCodes p ON p.PostalCode = l.PostalCode
        LEFT JOIN AspNetUsers u ON u.Id = n.UserId
        """;

    public IReadOnlyList<Need> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();

        // Dekkede behov sist, så mest kritisk først, så eldst først.
        return connection.Query<Need>(
            $"""
            {SelectNeeds}
            ORDER BY CASE WHEN n.Status = @Covered THEN 1 ELSE 0 END,
                     CASE n.Priority WHEN @Critical THEN 0 WHEN @High THEN 1 WHEN @Medium THEN 2 ELSE 3 END,
                     n.RegisteredAt
            """,
            new
            {
                Covered = NeedStatus.Covered,
                Critical = NeedPriority.Critical,
                High = NeedPriority.High,
                Medium = NeedPriority.Medium
            }).ToList();
    }

    public Need? GetById(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.QuerySingleOrDefault<Need>($"{SelectNeeds} WHERE n.Id = @Id", new { Id = id });
    }

    public int Add(Need need)
    {
        ArgumentNullException.ThrowIfNull(need);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        need.LocationId = LocationWriter.Save(
            connection, transaction, existingLocationId: null, need.Address, need.PostalCode, need.PostalPlace);

        var id = connection.ExecuteScalar<int>(
            """
            INSERT INTO Needs (Title, Description, Quantity, Priority, Status, RegisteredAt, Deadline,
                               UserId, OrganizationId, CategoryId, LocationId)
            VALUES (@Title, @Description, @Quantity, @Priority, @Status, @RegisteredAt, @Deadline,
                    @UserId, @OrganizationId, @CategoryId, @LocationId)
            RETURNING Id
            """,
            need,
            transaction);

        transaction.Commit();
        return id;
    }

    public bool Update(Need need)
    {
        ArgumentNullException.ThrowIfNull(need);

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var oldLocationId = connection.QuerySingleOrDefault<int?>(
            "SELECT LocationId FROM Needs WHERE Id = @Id", new { need.Id }, transaction);

        need.LocationId = LocationWriter.Save(
            connection, transaction, oldLocationId, need.Address, need.PostalCode, need.PostalPlace);

        // Status settes av tildelingene, ikke her. UserId og RegisteredAt endres ikke.
        var changedRows = connection.Execute(
            """
            UPDATE Needs
            SET Title = @Title, Description = @Description, Quantity = @Quantity, Priority = @Priority,
                Deadline = @Deadline, OrganizationId = @OrganizationId, CategoryId = @CategoryId, LocationId = @LocationId
            WHERE Id = @Id
            """,
            need,
            transaction);

        if (need.LocationId is null)
        {
            LocationWriter.DeleteIfUnused(connection, transaction, oldLocationId);
        }

        // Antallet kan ha endret seg.
        DapperAllocationRepository.UpdateNeedStatus(connection, transaction, need.Id);

        transaction.Commit();
        return changedRows == 1;
    }

    public bool Delete(int id)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var locationId = connection.QuerySingleOrDefault<int?>(
            "SELECT LocationId FROM Needs WHERE Id = @Id", new { Id = id }, transaction);

        // Ressursene som var tildelt skal bli ledige igjen.
        var resourceIds = connection.Query<int>(
            "SELECT ResourceId FROM Allocations WHERE NeedId = @Id", new { Id = id }, transaction).ToList();

        // Tildelingene slettes av ON DELETE CASCADE.
        var deleted = connection.Execute("DELETE FROM Needs WHERE Id = @Id", new { Id = id }, transaction) == 1;

        foreach (var resourceId in resourceIds)
        {
            DapperAllocationRepository.UpdateResourceStatus(connection, transaction, resourceId);
        }
        LocationWriter.DeleteIfUnused(connection, transaction, locationId);

        transaction.Commit();
        return deleted;
    }
}
