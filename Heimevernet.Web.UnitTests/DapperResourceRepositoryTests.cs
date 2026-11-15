using Dapper;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Tester SQL-en i DapperResourceRepository mot en SQLite-database: lagring, endring, sletting og adresser.
public class DapperResourceRepositoryTests
{
    // Kategori-Id-ene i SqliteTestDatabase: 1 = Mat, 2 = Vann ... 5 = Verktøy, 6 = Strøm og lading.
    private const int Water = 2;
    private const int Tools = 5;
    private const int Power = 6;

    [Fact]
    public void ReadMethods_ReturnPersistedResourcesOrderedById_WithCategoryAndOwner()
    {
        using var database = new SqliteTestDatabase();
        using (var connection = database.CreateConnection())
        {
            connection.Execute(
                """
                INSERT INTO AspNetUsers (Id, Email, Name, PhoneNumber, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
                VALUES ('ola-id', 'ola@test.no', 'Ola Nordmann', '12345678', 0, 0, 0, 0, 0)
                """);
            connection.Execute(
                "INSERT INTO Resources (Id, Name, Description, CategoryId, UserId) VALUES (@Id, @Name, @Description, @CategoryId, @UserId)",
                new[]
                {
                    new { Id = 12, Name = "Minibus", Description = "Nine seats", CategoryId = Tools, UserId = (string?)"ola-id" },
                    new { Id = 7, Name = "Water pump", Description = "Stored in depot A", CategoryId = Water, UserId = (string?)null }
                });
        }
        var repository = new DapperResourceRepository(database);

        var resources = repository.GetAll();
        var selectedResource = repository.GetById(12);
        var missingResource = repository.GetById(999);

        Assert.Collection(resources,
            resource =>
            {
                Assert.Equal(7, resource.Id);
                Assert.Equal("Water pump", resource.Name);
                Assert.Equal("Vann", resource.CategoryName);
                Assert.Null(resource.OwnerName);
            },
            resource =>
            {
                Assert.Equal(12, resource.Id);
                Assert.Equal("Minibus", resource.Name);
            });
        Assert.NotNull(selectedResource);
        Assert.Equal("Verktøy", selectedResource.CategoryName);
        Assert.Equal("Ola Nordmann", selectedResource.OwnerName);
        Assert.Equal("12345678", selectedResource.OwnerPhone);
        Assert.Equal(ResourceStatus.Available, selectedResource.Status);
        Assert.Null(missingResource);
    }

    [Fact]
    public void AddAndUpdate_SaveTheContactInfoOfTheResource()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        var id = repository.Add(new Resource
        {
            Name = "Aggregat", Description = "5 kW", CategoryId = Power, RegisteredAt = DateTime.UtcNow,
            ContactName = "Vaktmester Per", ContactPhone = "40000000", ContactEmail = "vakt@example.no"
        });
        var added = repository.GetById(id);

        Assert.NotNull(added);
        Assert.Equal("Vaktmester Per", added.ContactName);
        Assert.Equal("40000000", added.ContactPhone);
        Assert.Equal("vakt@example.no", added.ContactEmail);

        added.ContactName = null;
        added.ContactPhone = "99999999";
        added.ContactEmail = null;
        Assert.True(repository.Update(added));
        var updated = repository.GetById(id);

        Assert.NotNull(updated);
        Assert.Null(updated.ContactName);
        Assert.Equal("99999999", updated.ContactPhone);
        Assert.Null(updated.ContactEmail);
    }

    [Fact]
    public void Add_SavesAllFields_AndAddressGoesToLocationsAndPostalCodes()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        var id = repository.Add(new Resource
        {
            Name = "Aggregat", Description = "Strømaggregat 5 kW", CategoryId = Power, Quantity = 2,
            Status = ResourceStatus.Available, RegisteredAt = DateTime.UtcNow, UserId = "ola-id",
            Address = "Storgata 1", PostalCode = "4790", PostalPlace = "Lillesand"
        });

        var saved = repository.GetById(id);
        Assert.NotNull(saved);
        Assert.Equal("Aggregat", saved.Name);
        Assert.Equal("Strøm og lading", saved.CategoryName);
        Assert.Equal(2, saved.Quantity);
        Assert.Equal("ola-id", saved.UserId);
        Assert.NotNull(saved.LocationId);
        Assert.Equal("Storgata 1", saved.Address);
        Assert.Equal("4790", saved.PostalCode);
        Assert.Equal("Lillesand", saved.PostalPlace);

        using var connection = database.CreateConnection();
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations"));
        Assert.Equal("Lillesand", connection.ExecuteScalar<string>("SELECT PostalPlace FROM PostalCodes WHERE PostalCode = '4790'"));
    }

    [Fact]
    public void Add_WithoutAddress_SavesNoLocation()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        var id = repository.Add(new Resource { Name = "Vannkanne", Description = "10 liter", CategoryId = Water });

        Assert.Null(repository.GetById(id)!.LocationId);
        using var connection = database.CreateConnection();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations"));
    }

    [Fact]
    public void Add_SamePostalCodeTwice_KeepsOnePostalCodeRow()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        repository.Add(new Resource { Name = "A", Description = "a", CategoryId = Water, PostalCode = "4630", PostalPlace = "Kristiansand S" });
        repository.Add(new Resource { Name = "B", Description = "b", CategoryId = Water, PostalCode = "4630", PostalPlace = "Kristiansand S" });

        using var connection = database.CreateConnection();
        Assert.Equal(1, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM PostalCodes"));
        Assert.Equal(2, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations"));
    }

    [Fact]
    public void UpdateAndDelete_ChangeTheRowButKeepTheOwner()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);
        var id = repository.Add(new Resource { Name = "Vannpost", Description = "Tankbil", CategoryId = Water, UserId = "ola-id" });

        var updated = repository.Update(new Resource
        {
            Id = id, Name = "Vannpost Kampen", Description = "Tankbil, ta med kanner", CategoryId = Tools,
            Status = ResourceStatus.Unavailable, Quantity = 3, PostalCode = "0150", PostalPlace = "Oslo",
            UserId = "someone-else" // Skal ignoreres: Update endrer aldri eieren.
        });
        var afterUpdate = repository.GetById(id);
        var deleted = repository.Delete(id);
        var deletedAgain = repository.Delete(id);

        Assert.True(updated);
        Assert.NotNull(afterUpdate);
        Assert.Equal("Vannpost Kampen", afterUpdate.Name);
        Assert.Equal("Verktøy", afterUpdate.CategoryName);
        Assert.Equal(ResourceStatus.Unavailable, afterUpdate.Status);
        Assert.Equal(3, afterUpdate.Quantity);
        Assert.Equal("Oslo", afterUpdate.PostalPlace);
        Assert.Equal("ola-id", afterUpdate.UserId);
        Assert.True(deleted);
        Assert.False(deletedAgain);
        Assert.Null(repository.GetById(id));
        Assert.False(repository.Update(new Resource { Id = 999, Name = "x", Description = "x", CategoryId = Water }));

        // Da ressursen ble slettet, ble adressen også slettet.
        using var connection = database.CreateConnection();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations"));
    }

    [Fact]
    public void Update_RemovingTheAddress_DeletesTheOldLocation()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);
        var id = repository.Add(new Resource { Name = "Ly", Description = "Gymsal", CategoryId = 3, Address = "Skoleveien 2" });

        repository.Update(new Resource { Id = id, Name = "Ly", Description = "Gymsal", CategoryId = 3, Address = null });

        Assert.Null(repository.GetById(id)!.LocationId);
        using var connection = database.CreateConnection();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations"));
    }
}
