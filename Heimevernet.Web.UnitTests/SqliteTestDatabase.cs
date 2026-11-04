using System.Data.Common;
using Dapper;
using Heimevernet.Web.DataAccess;
using Microsoft.Data.Sqlite;

namespace Heimevernet.Web.UnitTests;

// A temporary SQLite database in memory that lives as long as this object.
// The app uses MariaDB, but the SQL in the repository and the Identity stores is plain enough
// to run on SQLite too, so the tests can check the queries without a database server.
// The tables mirror DataAccess/Sql/Schema.sql, written in SQLite syntax.
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

        CREATE TABLE Positions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Latitude REAL NOT NULL,
            Longitude REAL NOT NULL,
            Description TEXT NOT NULL,
            CreatedBy TEXT NULL,
            CreatedAtUtc TEXT NOT NULL);

        -- Same categories and order as Schema.sql, so Id 1 = Mat, 2 = Vann ... 7 = Informasjon.
        INSERT INTO Categories (Name, Description) VALUES
            ('Mat', NULL), ('Vann', NULL), ('Ly og varme', NULL), ('Helse', NULL),
            ('Verktøy', NULL), ('Strøm og lading', NULL), ('Informasjon', NULL);
        """;

    // A unique name per test, so tests never share data. "Cache=Shared" lets several connections see it.
    // "Foreign Keys=True": SQLite does not check foreign keys unless asked to.
    private readonly string _connectionString =
        $"Data Source=heimevernet-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Foreign Keys=True";
    private readonly SqliteConnection _keepAliveConnection;

    public SqliteTestDatabase()
    {
        // An in-memory database is deleted when its last connection closes, so this one stays open.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
        _keepAliveConnection.Execute(Schema);
    }

    public DbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public void Dispose() => _keepAliveConnection.Dispose();
}
