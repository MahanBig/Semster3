using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

public class ResourceControllerTests
{
    [Fact]
    public void Index_ExistingId_ReturnsSelectedResource()
    {
        var repository = new FakeResourceRepository(
            new Resource { Id = 7, Name = "Water pump", Description = "Stored in depot A", Type = "Equipment" },
            new Resource { Id = 12, Name = "Minibus", Description = "Nine seats", Type = "Vehicle" });
        var controller = new ResourceController(repository);

        var result = controller.Index(12);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ResourceViewModel>(viewResult.Model);
        Assert.Equal(12, model.Id);
        Assert.Equal("Minibus", model.Name);
        Assert.Equal("Nine seats", model.Description);
        Assert.Equal("Vehicle", model.Type);
    }

    [Fact]
    public void Index_MissingId_RedirectsToResourceList()
    {
        var controller = new ResourceController(new FakeResourceRepository());

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("ResourceHandler", redirect.ControllerName);
    }

    [Fact]
    public void Index_UnknownId_ReturnsNotFound()
    {
        var repository = new FakeResourceRepository(new Resource { Id = 7, Name = "Water pump" });
        var controller = new ResourceController(repository);

        var result = controller.Index(999);

        Assert.IsType<NotFoundResult>(result);
    }
}
