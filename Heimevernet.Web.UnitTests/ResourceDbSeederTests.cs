using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.UnitTests;

public class ResourceDbSeederTests
{
    [Fact]
    public void Seed_EmptyDatabase_PersistsThreeSampleResources()
    {
        using var connection = OpenConnection();
        using var context = CreateContext(connection);

        ResourceDbSeeder.Seed(context);
        context.ChangeTracker.Clear();

        var resources = context.Resources.OrderBy(resource => resource.Name).ToList();
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
        using var connection = OpenConnection();
        using var context = CreateContext(connection);
        ResourceDbSeeder.Seed(context);

        var resource = context.Resources.Single(resource => resource.Name == "Resource 1");
        var originalId = resource.Id;
        resource.Name = "Updated by the user";
        resource.Description = "Keep this description";
        context.SaveChanges();
        context.ChangeTracker.Clear();

        ResourceDbSeeder.Seed(context);
        ResourceDbSeeder.Seed(context);
        context.ChangeTracker.Clear();

        Assert.Equal(3, context.Resources.Count());
        var savedResource = context.Resources.Single(resource => resource.Id == originalId);
        Assert.Equal("Updated by the user", savedResource.Name);
        Assert.Equal("Keep this description", savedResource.Description);
        Assert.False(context.Resources.Any(resource => resource.Name == "Resource 1"));
    }

    [Fact]
    public void Seed_DatabaseWithExistingResource_DoesNotInsertSampleData()
    {
        using var connection = OpenConnection();
        using var context = CreateContext(connection);
        var existingResource = new Resource
        {
            Name = "Existing generator",
            Description = "Already registered in MariaDB",
            Type = "Equipment"
        };
        context.Resources.Add(existingResource);
        context.SaveChanges();
        var originalId = existingResource.Id;
        context.ChangeTracker.Clear();

        ResourceDbSeeder.Seed(context);
        context.ChangeTracker.Clear();

        var savedResource = Assert.Single(context.Resources.ToList());
        Assert.Equal(originalId, savedResource.Id);
        AssertSampleResource(savedResource, "Existing generator", "Already registered in MariaDB", "Equipment");
    }

    private static SqliteConnection OpenConnection()
    {
        // An open connection keeps this test's temporary database alive.
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static HeimevernetDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<HeimevernetDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new HeimevernetDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void AssertSampleResource(Resource resource, string name, string description, string type)
    {
        Assert.Equal(name, resource.Name);
        Assert.Equal(description, resource.Description);
        Assert.Equal(type, resource.Type);
    }
}
