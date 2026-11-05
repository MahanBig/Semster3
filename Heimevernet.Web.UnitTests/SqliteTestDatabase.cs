using System.Data.Common;
using Dapper;
using Heimevernet.Web.DataAccess;
using Microsoft.Data.Sqlite;

namespace Heimevernet.Web.UnitTests;

// En midlertidig SQLite-database i minnet som lever like lenge som dette objektet.
// Appen bruker MariaDB, men SQL-en i repositoryene og Identity-stores er enkel nok til å kjøre
// på SQLite også. Da kan testene sjekke spørringene uten en databaseserver.
// Tabellene er de samme som i DataAccess/Sql/Schema.sql, skrevet med SQLite-syntaks.
// Endrer du Schema.sql, må du endre her også.
internal sealed class SqliteTestDatabase : IDbConnectionFactory, IDisposable
{
    private const string Schema = """
        CREATE TABLE AspNetUsers (
            Id TEXT NOT NULL PRIMARY KEY,
            UserName TEXT NULL,
            NormalizedUserName TEXT NULL UNIQUE,
            Email TEXT NULL,
            NormalizedEmail TEXT NULL,
            EmailConfirmed INTEGER NOT NULL,
            PasswordHash TEXT NULL,
            SecurityStamp TEXT NULL,
            ConcurrencyStamp TEXT NULL,
            PhoneNumber TEXT NULL,
            PhoneNumberConfirmed INTEGER NOT NULL,
            TwoFactorEnabled INTEGER NOT NULL,
            LockoutEnd TEXT NULL,
            LockoutEnabled INTEGER NOT NULL,
            AccessFailedCount INTEGER NOT NULL,
            Name TEXT NULL,
            Consent INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);

        CREATE TABLE AspNetRoles (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NULL,
            NormalizedName TEXT NULL UNIQUE,
            ConcurrencyStamp TEXT NULL,
            Description TEXT NULL,
            AccessLevel INTEGER NOT NULL DEFAULT 0,
            RequiresVerification INTEGER NOT NULL DEFAULT 0);

        CREATE TABLE AspNetUserRoles (
            UserId TEXT NOT NULL,
            RoleId TEXT NOT NULL,
            VerifiedBy TEXT NULL,
            VerifiedAt TEXT NULL,
            PRIMARY KEY (UserId, RoleId));

        CREATE TABLE PostalCodes (
            PostalCode TEXT NOT NULL PRIMARY KEY,
            PostalPlace TEXT NOT NULL);

        CREATE TABLE Locations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Address TEXT NULL,
            PostalCode TEXT NULL REFERENCES PostalCodes (PostalCode),
            Latitude REAL NULL,
            Longitude REAL NULL,
            Description TEXT NULL);

        CREATE TABLE Categories (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL UNIQUE,
            Description TEXT NULL);

        CREATE TABLE Organizations (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Type TEXT NULL);

        CREATE TABLE Resources (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Description TEXT NOT NULL,
            Quantity INTEGER NOT NULL DEFAULT 1,
            Status TEXT NOT NULL DEFAULT 'tilgjengelig',
            RegisteredAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UserId TEXT NULL,
            CategoryId INTEGER NOT NULL REFERENCES Categories (Id),
            LocationId INTEGER NULL REFERENCES Locations (Id));

        CREATE TABLE Needs (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Title TEXT NOT NULL,
            Description TEXT NOT NULL,
            Quantity INTEGER NOT NULL DEFAULT 1,
            Priority TEXT NOT NULL DEFAULT 'middels',
            Status TEXT NOT NULL DEFAULT 'åpen',
            RegisteredAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            Deadline TEXT NULL,
            UserId TEXT NULL,
            OrganizationId INTEGER NULL REFERENCES Organizations (Id),
            CategoryId INTEGER NOT NULL REFERENCES Categories (Id),
            LocationId INTEGER NULL REFERENCES Locations (Id));

        CREATE TABLE Allocations (
            NeedId INTEGER NOT NULL REFERENCES Needs (Id) ON DELETE CASCADE,
            ResourceId INTEGER NOT NULL REFERENCES Resources (Id) ON DELETE CASCADE,
            Quantity INTEGER NOT NULL DEFAULT 1,
            AllocatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (NeedId, ResourceId));

        -- Same categories and order as Schema.sql, so Id 1 = Mat, 2 = Vann ... 7 = Informasjon.
        INSERT INTO Categories (Name, Description) VALUES
            ('Mat', NULL), ('Vann', NULL), ('Ly og varme', NULL), ('Helse', NULL),
            ('Verktøy', NULL), ('Strøm og lading', NULL), ('Informasjon', NULL);
        """;

    // Et unikt navn per test, så testene aldri deler data. "Cache=Shared" lar flere tilkoblinger se samme database.
    // "Foreign Keys=True": SQLite sjekker ikke fremmednøkler med mindre vi ber om det.
    private readonly string _connectionString =
        $"Data Source=heimevernet-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Foreign Keys=True";
    private readonly SqliteConnection _keepAliveConnection;

    public SqliteTestDatabase()
    {
        // En database i minnet slettes når den siste tilkoblingen lukkes, så denne holdes åpen.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
        _keepAliveConnection.Execute(Schema);
    }

    public DbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public void Dispose() => _keepAliveConnection.Dispose();
}
