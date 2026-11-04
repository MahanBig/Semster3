using Dapper;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class ResourceDbSeederTests
{
    [Fact]
    public void Seed_EmptyDatabase_PersistsThreeSampleResourcesWithCategories()
    {
        using var database = new SqliteTestDatabase();

        ResourceDbSeeder.Seed(database);

        var resources = ReadResources(database);
        Assert.Collection(resources,
            resource => AssertSampleResource(resource, "Eksempel: Drikkevann på kanne", "Vann"),
            resource => AssertSampleResource(resource, "Eksempel: Førstehjelpsskrin", "Helse"),
            resource => AssertSampleResource(resource, "Eksempel: Nødstrømsaggregat", "Strøm og lading"));
        Assert.All(resources, resource => Assert.True(resource.Id > 0));
        Assert.All(resources, resource => Assert.Null(resource.UserId));
        Assert.Equal(3, resources.Select(resource => resource.Id).Distinct().Count());
    }

    [Fact]
    public void Seed_RepeatedRuns_PreserveUserChangesAndDoNotAddDuplicates()
    {
        using var database = new SqliteTestDatabase();
        ResourceDbSeeder.Seed(database);
        var originalId = ReadResources(database).Single(resource => resource.Name == "Eksempel: Førstehjelpsskrin").Id;
        using (var connection = database.CreateConnection())
        {
            connection.Execute(
                "UPDATE Resources SET Name = @Name, Description = @Description WHERE Id = @Id",
                new { Id = originalId, Name = "Updated by the user", Description = "Keep this description" });
        }

        ResourceDbSeeder.Seed(database);
        ResourceDbSeeder.Seed(database);

        var resources = ReadResources(database);
        Assert.Equal(3, resources.Count);
        var savedResource = resources.Single(resource => resource.Id == originalId);
        Assert.Equal("Updated by the user", savedResource.Name);
        Assert.Equal("Keep this description", savedResource.Description);
        Assert.DoesNotContain(resources, resource => resource.Name == "Eksempel: Førstehjelpsskrin");
    }

    [Fact]
    public void Seed_DatabaseWithExistingResource_DoesNotInsertSampleData()
    {
        using var database = new SqliteTestDatabase();
        using (var connection = database.CreateConnection())
        {
            connection.Execute(
                "INSERT INTO Resources (Name, Description, CategoryId) VALUES (@Name, @Description, 5)",
                new { Name = "Existing generator", Description = "Already registered in MariaDB" });
        }

        ResourceDbSeeder.Seed(database);

        var savedResource = Assert.Single(ReadResources(database));
        AssertSampleResource(savedResource, "Existing generator", "Verktøy");
    }

    private static List<Resource> ReadResources(SqliteTestDatabase database)
    {
        using var connection = database.CreateConnection();
        return connection.Query<Resource>(
            """
            SELECT r.Id, r.Name, r.Description, r.UserId, c.Name AS CategoryName
            FROM Resources r INNER JOIN Categories c ON c.Id = r.CategoryId
            ORDER BY r.Name
            """).ToList();
    }

    private static void AssertSampleResource(Resource resource, string name, string categoryName)
    {
        Assert.Equal(name, resource.Name);
        Assert.Equal(categoryName, resource.CategoryName);
    }
}
