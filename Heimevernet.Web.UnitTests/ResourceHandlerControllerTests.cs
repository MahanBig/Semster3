using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

// Tester ressurslisten og skjemaet for ny ressurs i ResourceHandlerController.
public class ResourceHandlerControllerTests
{
    [Fact]
    public void Index_ReturnsResourcesFromRepository()
    {
        var repository = new FakeResourceRepository(
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", CategoryName = "Verktøy" },
            new Resource { Id = 12, Name = "Minibus", Description = "Nine seats", CategoryName = "Informasjon" });
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository());

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var models = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
        Assert.Collection(models,
            model =>
            {
                Assert.Equal(7, model.Id);
                Assert.Equal("Water pump", model.Name);
                Assert.Equal("Stored in depot A", model.Description);
                Assert.Equal("Verktøy", model.Category);
            },
            model =>
            {
                Assert.Equal(12, model.Id);
                Assert.Equal("Minibus", model.Name);
                Assert.Equal("Nine seats", model.Description);
                Assert.Equal("Informasjon", model.Category);
            });
    }

    [Fact]
    public void Index_EmptyRepository_ReturnsEmptyList()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository(), new FakeCategoryRepository());

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var models = Assert.IsType<List<ResourceViewModel>>(viewResult.Model);
        Assert.Empty(models);
    }

    [Fact]
    public void Create_Get_ReturnsEmptyForm()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository(), new FakeCategoryRepository());

        var result = controller.Create();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
        Assert.Equal(string.Empty, model.Name);
    }

    [Fact]
    public void Create_Post_ValidModel_SavesAndRedirectsToNewResource()
    {
        var repository = new FakeResourceRepository(
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", CategoryName = "Verktøy" });
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("admin@test.no");
        var model = new ResourceViewModel
        {
            Name = "  Lastebil ",
            Description = "Lastebil med tilhenger",
            Category = "Verktøy",
            Quantity = 2,
            PostalPlace = "Kristiansand",
            PostalCode = "4630",
            Address = "   " // Et tomt valgfritt felt lagres som null.
        };

        var result = controller.Create(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Resource", redirect.ControllerName);
        Assert.Equal(8, redirect.RouteValues!["id"]);
        var saved = Assert.Single(repository.Added);
        Assert.Equal("Lastebil", saved.Name);
        Assert.Equal(5, saved.CategoryId); // "Verktøy" har Id 5 i FakeCategoryRepository.
        Assert.Equal(2, saved.Quantity);
        Assert.Equal("Kristiansand", saved.PostalPlace);
        Assert.Equal("4630", saved.PostalCode);
        Assert.Null(saved.Address);
        Assert.Equal("admin@test.no", saved.UserId);
        Assert.Equal("Ressursen ble lagret.", controller.TempData["Message"]);
    }

    [Fact]
    public void Create_Post_InvalidModel_ShowsFormAgainAndDoesNotSave()
    {
        var repository = new FakeResourceRepository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("admin@test.no");
        controller.ModelState.AddModelError(nameof(ResourceViewModel.Name), "Skriv et navn.");
        var model = new ResourceViewModel { Description = "Uten navn", Category = "Verktøy" };

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
        Assert.Empty(repository.Added);
    }

    [Fact]
    public void Create_Post_UnknownCategory_ShowsFormAgainAndDoesNotSave()
    {
        var repository = new FakeResourceRepository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("ola@test.no");
        var model = new ResourceViewModel { Name = "Båt", Description = "Robåt", Category = "Finnes ikke" };

        var result = controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(repository.Added);
    }
}
