using Dapper;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class ResourceDbSeederTests
{
    [Fact]
    public void Seed_EmptyDatabase_PersistsThreeSampleResources()
    {
        using var database = new SqliteTestDatabase();

        ResourceDbSeeder.Seed(database);

        var resources = ReadResources(database);
        Assert.Collection(resources,
            resource => AssertSampleResource(resource, "Resource 1", "Db Description 1", "Type A"),
            resource => AssertSampleResource(resource, "Resource 2", "Db Description 2", "Type B"),
            resource => AssertSampleResource(resource, "Resource 3", "Db Description 3", "Type C"));
        Assert.All(resources, resource => Assert.True(resource.Id > 0));
        Assert.Equal(3, resources.Select(resource => resource.Id).Distinct().Count());
    }

    [Fact]
    public void Seed_RepeatedRuns_PreserveUserChangesAndDoNotAddDuplicates()
    {
        using var database = new SqliteTestDatabase();
        ResourceDbSeeder.Seed(database);
        var originalId = ReadResources(database).Single(resource => resource.Name == "Resource 1").Id;
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
        Assert.DoesNotContain(resources, resource => resource.Name == "Resource 1");
    }

    [Fact]
    public void Seed_DatabaseWithExistingResource_DoesNotInsertSampleData()
    {
        using var database = new SqliteTestDatabase();
        using (var connection = database.CreateConnection())
        {
            connection.Execute(
                "INSERT INTO Resources (Name, Description, Type) VALUES (@Name, @Description, @Type)",
                new { Name = "Existing generator", Description = "Already registered in MariaDB", Type = "Equipment" });
        }

        ResourceDbSeeder.Seed(database);

        var savedResource = Assert.Single(ReadResources(database));
        AssertSampleResource(savedResource, "Existing generator", "Already registered in MariaDB", "Equipment");
    }

    private static List<Resource> ReadResources(SqliteTestDatabase database)
    {
        using var connection = database.CreateConnection();
        return connection.Query<Resource>("SELECT Id, Name, Description, Type FROM Resources ORDER BY Name").ToList();
    }

    private static void AssertSampleResource(Resource resource, string name, string description, string type)
    {
        Assert.Equal(name, resource.Name);
        Assert.Equal(description, resource.Description);
        Assert.Equal(type, resource.Type);
    }
}
