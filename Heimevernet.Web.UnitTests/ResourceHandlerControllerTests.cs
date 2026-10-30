using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

public class ResourceHandlerControllerTests
{
    [Fact]
    public void Index_ReturnsResourcesFromRepository()
    {
        var repository = new FakeResourceRepository(
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", Type = "Equipment" },
            new Resource { Id = 12, Name = "Minibus", Description = "Nine seats", Type = "Vehicle" });
        var controller = new ResourceHandlerController(repository);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var models = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
        Assert.Collection(models,
            model =>
            {
                Assert.Equal(7, model.Id);
                Assert.Equal("Water pump", model.Name);
                Assert.Equal("Stored in depot A", model.Description);
                Assert.Equal("Equipment", model.Type);
            },
            model =>
            {
                Assert.Equal(12, model.Id);
                Assert.Equal("Minibus", model.Name);
                Assert.Equal("Nine seats", model.Description);
                Assert.Equal("Vehicle", model.Type);
            });
    }

    [Fact]
    public void Index_EmptyRepository_ReturnsEmptyList()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository());

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var models = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
        Assert.Empty(models);
    }

    [Fact]
    public void Create_Get_ReturnsEmptyForm()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository());

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
        Assert.Equal(string.Empty, model.Name);
    }

    [Fact]
    public void Create_Post_ValidModel_SavesAndRedirectsToNewResource()
    {
        var repository = new FakeResourceRepository(
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", Type = "Equipment" });
        var controller = new ResourceHandlerController(repository).WithUser("admin@test.no");
        var model = new ResourceViewModel
        {
            Name = "  Lastebil ",
            Description = "Lastebil med tilhenger",
            Type = "Kjøretøy",
            City = "Kristiansand",
            ZipCode = "4630",
            Address = "   " // Blank optional field is saved as null.
        };

        var result = controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Resource", redirect.ControllerName);
        Assert.Equal(8, redirect.RouteValues!["id"]);
        var saved = Assert.Single(repository.Added);
        Assert.Equal("Lastebil", saved.Name);
        Assert.Equal("Kristiansand", saved.City);
        Assert.Equal("4630", saved.ZipCode);
        Assert.Null(saved.Address);
        Assert.Equal("Ressursen ble lagret.", controller.TempData["Message"]);
    }

    [Fact]
    public void Create_Post_InvalidModel_ShowsFormAgainAndDoesNotSave()
    {
        var repository = new FakeResourceRepository();
        var controller = new ResourceHandlerController(repository).WithUser("admin@test.no");
        controller.ModelState.AddModelError(nameof(ResourceViewModel.Name), "Skriv et navn.");
        var model = new ResourceViewModel { Description = "Uten navn", Type = "Utstyr" };

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Empty(repository.Added);
    }
}
