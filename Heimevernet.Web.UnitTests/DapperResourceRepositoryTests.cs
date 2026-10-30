using Dapper;
using Heimevernet.Web.DataAccess;

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
}
