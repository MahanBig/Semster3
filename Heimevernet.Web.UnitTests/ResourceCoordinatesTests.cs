using System.ComponentModel.DataAnnotations;
using Dapper;
using Heimevernet.Web.Controllers;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

// Tester punktet på kartet (Latitude/Longitude i Locations): lagring, skjemaet, valideringen og eksempeldataene.
public class ResourceCoordinatesTests
{
    private const int Water = 2; // Kategori-Id-en til Vann i SqliteTestDatabase.

    [Fact]
    public void Repository_SavesUpdatesAndClearsThePoint()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperResourceRepository(database);

        var id = repository.Add(new Resource
        {
            Name = "Vannpost", Description = "Tankbil", CategoryId = Water,
            Latitude = 58.146400, Longitude = 7.995900
        });
        var saved = repository.GetById(id);

        Assert.NotNull(saved);
        Assert.Equal(58.1464, saved.Latitude!.Value, 6);
        Assert.Equal(7.9959, saved.Longitude!.Value, 6);

        // Flytt punktet. Samme rad i Locations brukes, det lages ikke en ny.
        saved.Latitude = 58.2475;
        saved.Longitude = 8.3778;
        repository.Update(saved);
        var moved = repository.GetById(id)!;
        Assert.Equal(58.2475, moved.Latitude!.Value, 6);
        Assert.Equal(8.3778, moved.Longitude!.Value, 6);
        Assert.Equal(1, CountLocations(database));

        // Fjern punktet. Uten adresse og punkt slettes raden i Locations.
        moved.Latitude = null;
        moved.Longitude = null;
        repository.Update(moved);
        var cleared = repository.GetById(id)!;
        Assert.Null(cleared.Latitude);
        Assert.Null(cleared.Longitude);
        Assert.Equal(0, CountLocations(database));
    }

    [Fact]
    public void CreateGet_FromTheMap_PrefillsThePoint()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository(), new FakeCategoryRepository());

        var model = Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(controller.Create(58.1234567, 8.7654321)).Model);

        Assert.Equal(58.123457, model.Latitude); // Rundet til 6 desimaler, som i databasen.
        Assert.Equal(8.765432, model.Longitude);
    }

    [Fact]
    public void CreateGet_InvalidPoint_IsIgnored()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository(), new FakeCategoryRepository());

        var model = Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(controller.Create(123, 8)).Model);

        Assert.Null(model.Latitude);
        Assert.Null(model.Longitude);
    }

    [Fact]
    public void CreatePost_SavesThePoint()
    {
        var repository = new FakeResourceRepository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("ola@test.no", AppRoles.User);

        controller.Create(new ResourceViewModel
        {
            Name = "Aggregat", Description = "5 kW", Category = "Strøm og lading",
            Latitude = 58.14641234, Longitude = 7.99591234
        });

        var saved = Assert.Single(repository.Added);
        Assert.Equal(58.146412, saved.Latitude);
        Assert.Equal(7.995912, saved.Longitude);
    }

    [Theory]
    [InlineData(58.1, null)]
    [InlineData(null, 8.0)]
    public void Validation_OnlyHalfAPoint_GivesError(double? latitude, double? longitude)
    {
        var model = new ResourceViewModel
        {
            Name = "Aggregat", Description = "5 kW", Category = "Strøm og lading",
            Latitude = latitude, Longitude = longitude
        };

        var errors = model.Validate(new ValidationContext(model)).ToList();

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(ResourceViewModel.Latitude)));
    }

    [Fact]
    public void Seeder_GivesTheSampleResourcesAPointAndAnAddress()
    {
        using var database = new SqliteTestDatabase();

        ResourceDbSeeder.Seed(database);

        var resources = new DapperResourceRepository(database).GetAll();
        Assert.Equal(3, resources.Count);
        Assert.All(resources, resource =>
        {
            Assert.NotNull(resource.Latitude);
            Assert.NotNull(resource.Longitude);
            Assert.Equal("Kristiansand", resource.PostalPlace);
        });
    }

    private static int CountLocations(SqliteTestDatabase database)
    {
        using var connection = database.CreateConnection();
        return connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Locations");
    }
}
