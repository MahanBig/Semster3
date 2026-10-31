using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsResourcesAndPositionsForTheMap()
    {
        var positions = new FakePositionRepository();
        positions.Add(new Position { Latitude = 58.1, Longitude = 8.0, Description = "Vannpost" });
        var resources = new FakeResourceRepository(
            new Resource { Id = 3, Name = "Aggregat", Description = "5 kW", Type = "Utstyr" });
        var controller = new HomeController(positions, resources);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MapPageViewModel>(viewResult.Model);
        var resource = Assert.Single(model.Resources);
        Assert.Equal(3, resource.Id);
        Assert.Equal("Aggregat", resource.Name);
        Assert.Single(model.Positions);
    }

    [Fact]
    public void CorrectMap_Post_ValidPosition_SavesAndRedirectsToOverview()
    {
        var repository = new FakePositionRepository();
        var controller = new HomeController(repository, new FakeResourceRepository()).WithUser("ola@test.no");
        var model = new PositionModel { Latitude = "58.2475", Longitude = "8.3778", Description = " Brannstasjon " };

        var result = controller.CorrectMap(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(HomeController.CorrectionOverview), redirect.ActionName);
        var saved = Assert.Single(repository.Added);
        Assert.Equal(58.2475, saved.Latitude);
        Assert.Equal(8.3778, saved.Longitude);
        Assert.Equal("Brannstasjon", saved.Description);
        Assert.Equal("ola@test.no", saved.CreatedBy);
        Assert.InRange(saved.CreatedAtUtc, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1));
    }

    [Theory]
    [InlineData("91", "10")]   // Latitude out of range.
    [InlineData("60", "181")]  // Longitude out of range.
    [InlineData("abc", "10")]  // Not a number.
    public void CorrectMap_Post_InvalidCoordinates_ShowsFormAndDoesNotSave(string latitude, string longitude)
    {
        var repository = new FakePositionRepository();
        var controller = new HomeController(repository, new FakeResourceRepository()).WithUser();
        var model = new PositionModel { Latitude = latitude, Longitude = longitude, Description = "Test" };

        var result = controller.CorrectMap(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public void CorrectionOverview_ReturnsSavedPositions()
    {
        var repository = new FakePositionRepository();
        repository.Add(new Position { Latitude = 1, Longitude = 2, Description = "A" });
        var controller = new HomeController(repository, new FakeResourceRepository());

        var result = controller.CorrectionOverview();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IReadOnlyCollection<Position>>(viewResult.Model);
        Assert.Single(model);
    }
}
