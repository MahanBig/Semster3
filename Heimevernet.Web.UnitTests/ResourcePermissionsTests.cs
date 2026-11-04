using System.Security.Claims;
using Heimevernet.Web.Authorization;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class ResourcePermissionsTests
{
    private static readonly Resource OwnedByOla = new() { Id = 1, Name = "Aggregat", CreatedBy = "ola@test.no" };
    private static readonly Resource SeedData = new() { Id = 2, Name = "Resource 1", CreatedBy = null };

    private static ClaimsPrincipal User(string name, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, name) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void Owner_CanModify_IgnoringCase() =>
        Assert.True(ResourcePermissions.CanModify(User("OLA@test.no", AppRoles.User), OwnedByOla));

    [Fact]
    public void OtherUser_CannotModify() =>
        Assert.False(ResourcePermissions.CanModify(User("kari@test.no", AppRoles.User), OwnedByOla));

    [Fact]
    public void Admin_CanModifyEverything()
    {
        var admin = User("admin@test.no", AppRoles.Admin);
        Assert.True(ResourcePermissions.CanModify(admin, OwnedByOla));
        Assert.True(ResourcePermissions.CanModify(admin, SeedData));
    }

    [Fact]
    public void NormalUser_CannotModifyResourceWithoutOwner() =>
        Assert.False(ResourcePermissions.CanModify(User("ola@test.no"), SeedData));

    [Fact]
    public void AnonymousUser_CannotModify()
    {
        Assert.False(ResourcePermissions.CanModify(null, OwnedByOla));
        Assert.False(ResourcePermissions.CanModify(new ClaimsPrincipal(new ClaimsIdentity()), OwnedByOla));
    }
}
