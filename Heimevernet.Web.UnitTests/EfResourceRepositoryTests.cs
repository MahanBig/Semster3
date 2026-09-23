using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.UnitTests;

public class EfResourceRepositoryTests
{
    [Fact]
    public void ReadMethods_ReturnPersistedResourcesWithoutTrackingChanges()
    {
        // Keep SQLite open so both queries read the same temporary database.
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<HeimevernetDbContext>()
            .UseSqlite(connection)
            .Options;
        using var context = new HeimevernetDbContext(options);
        context.Database.EnsureCreated();
        context.Resources.AddRange(
            new Resource { Id = 12, Name = "Minibus", Description = "Nine seats", Type = "Vehicle" },
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", Type = "Equipment" });
        context.SaveChanges();
        context.ChangeTracker.Clear();
        var repository = new EfResourceRepository(context);

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
        Assert.Empty(context.ChangeTracker.Entries());
    }
}
