using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

// Krav: en innlogget bruker kan legge til, endre og slette ressurser.
// Eier kan endre/slette sine egne, admin kan endre/slette alle, andre får "Ingen tilgang" (Forbid).
public class ResourceEditDeleteTests
{
    private static FakeResourceRepository Repository() => new(
        new Resource { Id = 1, Name = "Aggregat", Description = "5 kW", CategoryName = "Strøm og lading", UserId = "ola@test.no" },
        new Resource { Id = 2, Name = "Eksempel", Description = "Seed", CategoryName = "Vann", UserId = null });

    private static ResourceViewModel ValidModel(string name = "Aggregat 10 kW") =>
        new() { Name = name, Description = "Større aggregat", Category = "Strøm og lading", PostalCode = "4790", PostalPlace = "Lillesand" };

    [Fact]
    public void Create_Post_SetsLoggedInUserAsOwner()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("kari@test.no", AppRoles.User);

        controller.Create(ValidModel("Vannpost"));

        Assert.Equal("kari@test.no", Assert.Single(repository.Added).UserId);
    }

    [Fact]
    public void Edit_Get_Owner_GetsFormWithCurrentValues()
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository()).WithUser("ola@test.no", AppRoles.User);

        var view = Assert.IsType<ViewResult>(controller.Edit(1));

        var model = Assert.IsType<ResourceViewModel>(view.Model);
        Assert.Equal("Aggregat", model.Name);
    }

    [Fact]
    public void Edit_Get_OtherUser_IsForbidden()
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository()).WithUser("kari@test.no", AppRoles.User);

        Assert.IsType<ForbidResult>(controller.Edit(1));
    }

    [Fact]
    public void Edit_Get_UnknownId_ReturnsNotFound()
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository()).WithUser("admin@test.no", AppRoles.Admin);

        Assert.IsType<NotFoundResult>(controller.Edit(999));
    }

    [Fact]
    public void Edit_Post_Owner_SavesChangesAndKeepsOwner()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("ola@test.no", AppRoles.User);

        var result = controller.Edit(1, ValidModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Resource", redirect.ControllerName);
        Assert.Equal(1, redirect.RouteValues!["id"]);
        var saved = Assert.Single(repository.Updated);
        Assert.Equal("Aggregat 10 kW", saved.Name);
        Assert.Equal("Lillesand", saved.PostalPlace);
        Assert.Equal("ola@test.no", saved.UserId);
        Assert.Equal("Endringene ble lagret.", controller.TempData["Message"]);
    }

    [Fact]
    public void Edit_Post_OtherUser_IsForbiddenAndNothingIsSaved()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("kari@test.no", AppRoles.User);

        Assert.IsType<ForbidResult>(controller.Edit(1, ValidModel()));
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public void Edit_Post_InvalidModel_ShowsFormAgain()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("ola@test.no", AppRoles.User);
        controller.ModelState.AddModelError(nameof(ResourceViewModel.Name), "Skriv et navn.");

        var view = Assert.IsType<ViewResult>(controller.Edit(1, ValidModel(name: "")));

        Assert.Equal(1, Assert.IsType<ResourceViewModel>(view.Model).Id);
        Assert.Empty(repository.Updated);
    }

    [Fact]
    public void Edit_Post_Admin_CanEditResourceWithoutOwner()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("admin@test.no", AppRoles.Admin);

        Assert.IsType<RedirectToActionResult>(controller.Edit(2, ValidModel("Oppdatert seed")));
        Assert.Null(Assert.Single(repository.Updated).UserId);
    }

    [Fact]
    public void Delete_Get_OtherUser_IsForbidden()
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository()).WithUser("kari@test.no", AppRoles.User);

        Assert.IsType<ForbidResult>(controller.Delete(1));
    }

    [Fact]
    public void DeleteConfirmed_Owner_DeletesAndRedirectsToList()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("ola@test.no", AppRoles.User);

        var redirect = Assert.IsType<RedirectToActionResult>(controller.DeleteConfirmed(1));

        Assert.Equal(nameof(ResourceHandlerController.Index), redirect.ActionName);
        Assert.Equal([1], repository.Deleted);
        Assert.Null(repository.GetById(1));
        Assert.Equal("Ressursen «Aggregat» ble slettet.", controller.TempData["Message"]);
    }

    [Fact]
    public void DeleteConfirmed_OtherUser_IsForbiddenAndNothingIsDeleted()
    {
        var repository = Repository();
        var controller = new ResourceHandlerController(repository, new FakeCategoryRepository()).WithUser("kari@test.no", AppRoles.User);

        Assert.IsType<ForbidResult>(controller.DeleteConfirmed(1));
        Assert.Empty(repository.Deleted);
    }

    [Fact]
    public void DeleteConfirmed_UnknownId_ReturnsNotFound()
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository()).WithUser("admin@test.no", AppRoles.Admin);

        Assert.IsType<NotFoundResult>(controller.DeleteConfirmed(999));
    }

    [Fact]
    public void ResourcePage_ShowsEditButtonsOnlyForOwner()
    {
        var repository = Repository();

        var asOwner = new ResourceController(repository).WithUser("ola@test.no", AppRoles.User).Index(1);
        var asOther = new ResourceController(repository).WithUser("kari@test.no", AppRoles.User).Index(1);

        Assert.True(Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(asOwner).Model).CanModify);
        Assert.False(Assert.IsType<ResourceViewModel>(Assert.IsType<ViewResult>(asOther).Model).CanModify);
    }
}
