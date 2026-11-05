using System.Data.Common;
using Dapper;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Tildelinger: hvilke ressurser som er gitt til hvilke behov (tabellen Allocations).
// Statusene på behov og ressurs oppdateres her, i samme transaksjon som tildelingen,
// så de aldri blir stående i utakt med tildelingene.
public class DapperAllocationRepository(IDbConnectionFactory connectionFactory) : IAllocationRepository
{
    // Tildelingene til ett behov, med navnet på ressursen og eieren (til listen på detaljsiden).
    public IReadOnlyList<Allocation> GetForNeed(int needId)
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Allocation>(
            """
            SELECT a.NeedId, a.ResourceId, a.Quantity, a.AllocatedAt,
                   r.Name AS ResourceName, r.UserId AS ResourceOwnerId,
                   COALESCE(u.Name, u.Email) AS ResourceOwnerName
            FROM Allocations a
            INNER JOIN Resources r ON r.Id = a.ResourceId
            LEFT JOIN AspNetUsers u ON u.Id = r.UserId
            WHERE a.NeedId = @NeedId
            ORDER BY a.AllocatedAt
            """,
            new { NeedId = needId }).ToList();
    }

    public bool Allocate(int needId, int resourceId, int quantity)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        // Ingenting lagres før Commit til slutt. Går noe galt før det, ruller "using" tilbake alt.
        using var transaction = connection.BeginTransaction();

        // Sjekker først, så et duplikat ikke gir databasefeil (primærnøkkelen er NeedId + ResourceId).
        var exists = connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Allocations WHERE NeedId = @NeedId AND ResourceId = @ResourceId",
            new { NeedId = needId, ResourceId = resourceId },
            transaction) > 0;
        if (exists)
        {
            return false;
        }

        connection.Execute(
            """
            INSERT INTO Allocations (NeedId, ResourceId, Quantity, AllocatedAt)
            VALUES (@NeedId, @ResourceId, @Quantity, @AllocatedAt)
            """,
            new { NeedId = needId, ResourceId = resourceId, Quantity = quantity, AllocatedAt = DateTime.UtcNow },
            transaction);

        UpdateNeedStatus(connection, transaction, needId);
        UpdateResourceStatus(connection, transaction, resourceId);

        transaction.Commit();
        return true;
    }

    public bool Remove(int needId, int resourceId)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        var deleted = connection.Execute(
            "DELETE FROM Allocations WHERE NeedId = @NeedId AND ResourceId = @ResourceId",
            new { NeedId = needId, ResourceId = resourceId },
            transaction) == 1;

        UpdateNeedStatus(connection, transaction, needId);
        UpdateResourceStatus(connection, transaction, resourceId);

        transaction.Commit();
        return deleted;
    }

    // Regner ut statusen til behovet fra tildelingene:
    //   dekket           = tildelt antall er minst det som trengs
    //   under_behandling = noe er tildelt, men ikke nok
    //   åpen             = ingenting er tildelt
    // internal fordi DapperNeedRepository også bruker den når et behov endres.
    internal static void UpdateNeedStatus(DbConnection connection, DbTransaction transaction, int needId)
    {
        connection.Execute(
            """
            UPDATE Needs
            SET Status = CASE
                WHEN (SELECT COALESCE(SUM(Quantity), 0) FROM Allocations WHERE NeedId = @NeedId) >= Quantity THEN @Covered
                WHEN (SELECT COUNT(*) FROM Allocations WHERE NeedId = @NeedId) > 0 THEN @InProgress
                ELSE @Open
            END
            WHERE Id = @NeedId
            """,
            new { NeedId = needId, Covered = NeedStatus.Covered, InProgress = NeedStatus.InProgress, Open = NeedStatus.Open },
            transaction);
    }

    // i_bruk hvis ressursen er tildelt minst ett behov, ellers tilgjengelig.
    // En ressurs som eieren har satt til "utilgjengelig", endres ikke.
    internal static void UpdateResourceStatus(DbConnection connection, DbTransaction transaction, int resourceId)
    {
        connection.Execute(
            """
            UPDATE Resources
            SET Status = CASE
                WHEN EXISTS (SELECT 1 FROM Allocations WHERE ResourceId = @ResourceId) THEN @InUse
                ELSE @Available
            END
            WHERE Id = @ResourceId AND Status <> @Unavailable
            """,
            new
            {
                ResourceId = resourceId,
                InUse = ResourceStatus.InUse,
                Available = ResourceStatus.Available,
                Unavailable = ResourceStatus.Unavailable
            },
            transaction);
    }
}
