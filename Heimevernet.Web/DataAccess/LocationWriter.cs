using System.Data.Common;
using Dapper;

namespace Heimevernet.Web.DataAccess;

// Lagrer adressen til en ressurs eller et behov. Hver ressurs og hvert behov har sin egen rad i Locations.
// Kalles bare inne i en transaksjon fra repositoryene, derfor tar metodene inn tilkobling og transaksjon.
internal static class LocationWriter
{
    // Lager eller oppdaterer adressen og returnerer Id-en til raden i Locations.
    // Returnerer null hvis adressefeltene er tomme.
    public static int? Save(
        DbConnection connection,
        DbTransaction transaction,
        int? existingLocationId,
        string? address,
        string? postalCode,
        string? postalPlace)
    {
        if (address is null && postalCode is null)
        {
            return null;
        }

        // Postnummeret må finnes i PostalCodes før Locations kan peke på det.
        if (postalCode is not null)
        {
            connection.Execute(
                """
                INSERT INTO PostalCodes (PostalCode, PostalPlace)
                SELECT @PostalCode, @PostalPlace
                WHERE NOT EXISTS (SELECT 1 FROM PostalCodes WHERE PostalCode = @PostalCode)
                """,
                new { PostalCode = postalCode, PostalPlace = postalPlace ?? string.Empty },
                transaction);
        }

        if (existingLocationId is { } id)
        {
            connection.Execute(
                "UPDATE Locations SET Address = @Address, PostalCode = @PostalCode WHERE Id = @Id",
                new { Id = id, Address = address, PostalCode = postalCode },
                transaction);
            return id;
        }

        return connection.ExecuteScalar<int>(
            "INSERT INTO Locations (Address, PostalCode) VALUES (@Address, @PostalCode) RETURNING Id",
            new { Address = address, PostalCode = postalCode },
            transaction);
    }

    // Sletter adressen, men bare hvis ingen ressurs eller behov peker på den lenger.
    public static void DeleteIfUnused(DbConnection connection, DbTransaction transaction, int? locationId)
    {
        if (locationId is null)
        {
            return;
        }

        connection.Execute(
            """
            DELETE FROM Locations
            WHERE Id = @Id
              AND NOT EXISTS (SELECT 1 FROM Resources WHERE LocationId = @Id)
              AND NOT EXISTS (SELECT 1 FROM Needs WHERE LocationId = @Id)
            """,
            new { Id = locationId },
            transaction);
    }
}
