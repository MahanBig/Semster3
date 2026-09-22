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
    public void Create_ReturnsViewWithSubmittedModel()
    {
        var controller = new ResourceHandlerController(new FakeResourceRepository());
        var model = new ResourceViewModel
        {
            Name = "Lastebil",
            Description = "Lastebil med tilhenger",
            Type = "Kjøretøy"
        };

        var result = controller.Create(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(model, viewResult.Model);
    }
}
