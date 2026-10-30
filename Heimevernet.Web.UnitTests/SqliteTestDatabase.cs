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
        CREATE TABLE Resources (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Description TEXT NOT NULL,
            Type TEXT NOT NULL,
            Address TEXT NULL,
            City TEXT NULL,
            ZipCode TEXT NULL,
            OwnerTelephoneNumber TEXT NULL);

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
            AccessFailedCount INTEGER NOT NULL);

        CREATE TABLE AspNetRoles (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NULL,
            NormalizedName TEXT NULL UNIQUE,
            ConcurrencyStamp TEXT NULL);

        CREATE TABLE AspNetUserRoles (
            UserId TEXT NOT NULL,
            RoleId TEXT NOT NULL,
            PRIMARY KEY (UserId, RoleId));
        """;

    // A unique name per test, so tests never share data. "Cache=Shared" lets several connections see it.
    private readonly string _connectionString = $"Data Source=heimevernet-test-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
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
