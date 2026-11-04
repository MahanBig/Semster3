using Dapper;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class DapperResourceRepositoryTests
{
    [Fact]
    public void ReadMethods_ReturnPersistedResourcesOrderedById()
    {
        using var database = new SqliteTestDatabase();
        using (var connection = database.CreateConnection())
        {
            connection.Execute(
                "INSERT INTO Resources (Id, Name, Description, Type) VALUES (@Id, @Name, @Description, @Type)",
                new[]
                {
                    new { Id = 12, Name = "Minibus", Description = "Nine seats", Type = "Vehicle" },
                    new { Id = 7, Name = "Water pump", Description = "Stored in depot A", Type = "Equipment" }
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
                Assert.Equal("Stored in depot A", resource.Description);
                Assert.Equal("Equipment", resource.Type);
            },
            resource =>
            {
                Assert.Equal(12, resource.Id);
                Assert.Equal("Minibus", resource.Name);
            });
        Assert.NotNull(selectedResource);
        Assert.Equal(12, selectedResource.Id);
        Assert.Equal("Minibus", selectedResource.Name);
        Assert.Equal("Nine seats", selectedResource.Description);
        Assert.Equal("Vehicle", selectedResource.Type);
        Assert.Null(missingResource);
    }

    [Fact]
    public void UpdateAndDelete_ChangeTheRowButKeepTheOwner()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);
        var id = repository.Add(new Resource { Name = "Vannpost", Description = "Tankbil", Type = "Vann", CreatedBy = "ola@test.no" });

        var updated = repository.Update(new Resource
        {
            Id = id, Name = "Vannpost Kampen", Description = "Tankbil, ta med kanner", Type = "Vann", City = "Oslo",
            CreatedBy = "someone-else@test.no" // Must be ignored: Update never changes the owner.
        });
        var afterUpdate = repository.GetById(id);
        var deleted = repository.Delete(id);
        var deletedAgain = repository.Delete(id);

        Assert.True(updated);
        Assert.NotNull(afterUpdate);
        Assert.Equal("Vannpost Kampen", afterUpdate.Name);
        Assert.Equal("Oslo", afterUpdate.City);
        Assert.Equal("ola@test.no", afterUpdate.CreatedBy);
        Assert.True(deleted);
        Assert.False(deletedAgain);
        Assert.Null(repository.GetById(id));
        Assert.False(repository.Update(new Resource { Id = 999, Name = "x", Description = "x", Type = "x" }));
    }

    [Fact]
    public void Add_SavesAllFieldsAndReturnsNewId()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        var id = repository.Add(new Resource
        {
            Name = "Aggregat", Description = "Strømaggregat 5 kW", Type = "Utstyr",
            Address = "Storgata 1", City = "Lillesand", ZipCode = "4790", OwnerTelephoneNumber = "12345678",
            CreatedBy = "ola@test.no"
        });

        var saved = repository.GetById(id);
        Assert.NotNull(saved);
        Assert.Equal("Aggregat", saved.Name);
        Assert.Equal("Storgata 1", saved.Address);
        Assert.Equal("Lillesand", saved.City);
        Assert.Equal("4790", saved.ZipCode);
        Assert.Equal("12345678", saved.OwnerTelephoneNumber);
        Assert.Equal("ola@test.no", saved.CreatedBy);
    }
}
