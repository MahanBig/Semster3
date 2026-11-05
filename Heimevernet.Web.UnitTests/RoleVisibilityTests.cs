using System.Security.Claims;
using Heimevernet.Web.Authorization;
using Heimevernet.Web.Controllers;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.UnitTests;

// Rollene bestemmer hva man ser: Privat ser bare sine egne ressurser, Offentlig og Admin ser alle.
public class RoleVisibilityTests
{
    private static FakeResourceRepository Repository() => new(
        new Resource { Id = 1, Name = "Olas aggregat", Description = "5 kW", CategoryName = "Strøm og lading", UserId = "ola@test.no" },
        new Resource { Id = 2, Name = "Karis vannkanne", Description = "10 l", CategoryName = "Vann", UserId = "kari@test.no" },
        new Resource { Id = 3, Name = "Eksempel uten eier", Description = "Seed", CategoryName = "Helse", UserId = null });

    [Theory]
    [InlineData(AppRoles.Private, new[] { 1 })]
    [InlineData(AppRoles.Public, new[] { 1, 2, 3 })]
    [InlineData(AppRoles.Admin, new[] { 1, 2, 3 })]
    public void ResourceList_ShowsOnlyWhatTheRoleMaySee(string role, int[] expectedIds)
    {
        var controller = new ResourceHandlerController(Repository(), new FakeCategoryRepository(), new FakeResourceImageRepository()).WithUser("ola@test.no", role);

        var models = Assert.IsType<List<ResourceViewModel>>(Assert.IsType<ViewResult>(controller.Index()).Model);

        Assert.Equal(expectedIds, models.Select(model => model.Id!.Value).Order());
    }

    [Fact]
    public void Map_Private_ShowsOnlyOwnResources()
    {
        var controller = new HomeController(Repository()).WithUser("kari@test.no", AppRoles.Private);

        var model = Assert.IsType<MapPageViewModel>(Assert.IsType<ViewResult>(controller.Index()).Model);

        Assert.Equal("Karis vannkanne", Assert.Single(model.Resources).Name);
    }

    [Fact]
    public void ResourcePage_Private_OthersResourceGives404()
    {
        var controller = new ResourceController(Repository(), new FakeResourceImageRepository()).WithUser("kari@test.no", AppRoles.Private);

        Assert.IsType<NotFoundResult>(controller.Index(1));
        Assert.IsType<ViewResult>(controller.Index(2)); // Sin egen kan hun se.
    }

    [Fact]
    public void Public_SeesAllButCanOnlyModifyOwn()
    {
        var kari = Principal("kari@test.no", AppRoles.Public);
        var olas = new Resource { UserId = "ola@test.no" };

        Assert.True(ResourcePermissions.CanView(kari, olas));
        Assert.False(ResourcePermissions.CanModify(kari, olas)); // Endre og slette andres: bare admin.
    }

    [Fact]
    public void NotLoggedIn_SeesNothing()
    {
        Assert.False(ResourcePermissions.CanSeeAll(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.False(ResourcePermissions.CanView(null, new Resource { UserId = null }));
    }

    private static ClaimsPrincipal Principal(string id, string role) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role)], "Test"));
}
