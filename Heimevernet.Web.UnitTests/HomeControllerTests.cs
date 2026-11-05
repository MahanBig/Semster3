using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

// Tester at forsiden får ressursene fra repositoryet.
public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsResourcesForTheMapPage()
    {
        var resources = new FakeResourceRepository(
            new Resource { Id = 3, Name = "Aggregat", Description = "5 kW", CategoryName = "Strøm og lading" });
        var controller = new HomeController(resources).WithUser("ola@test.no", Heimevernet.Web.Models.AppRoles.Public);

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<MapPageViewModel>(viewResult.Model);
        var resource = Assert.Single(model.Resources);
        Assert.Equal(3, resource.Id);
        Assert.Equal("Aggregat", resource.Name);
    }
}
