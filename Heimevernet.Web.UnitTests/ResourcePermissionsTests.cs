using System.Security.Claims;
using Heimevernet.Web.Authorization;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class ResourcePermissionsTests
{
    private static readonly Resource OwnedByOla = new() { Id = 1, Name = "Aggregat", UserId = "ola-id" };
    private static readonly Resource SeedData = new() { Id = 2, Name = "Eksempel", UserId = null };
    private static readonly Need NeedByOla = new() { Id = 1, Title = "Vann", UserId = "ola-id" };

    private static ClaimsPrincipal User(string id, params string[] roles)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, id + "@test.no"), new(ClaimTypes.NameIdentifier, id) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    [Fact]
    public void Owner_CanModify() =>
        Assert.True(ResourcePermissions.CanModify(User("ola-id", AppRoles.User), OwnedByOla));

    [Fact]
    public void Owner_IsComparedById_NotByName()
    {
        // Same e-mail in the Name claim, but another Id: not the owner.
        var claims = new List<Claim> { new(ClaimTypes.Name, "ola-id@test.no"), new(ClaimTypes.NameIdentifier, "other-id") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        Assert.False(ResourcePermissions.CanModify(user, OwnedByOla));
    }

    [Fact]
    public void NeedOwner_CanModifyNeed_OtherUserCannot()
    {
        Assert.True(ResourcePermissions.CanModify(User("ola-id", AppRoles.User), NeedByOla));
        Assert.False(ResourcePermissions.CanModify(User("kari-id", AppRoles.User), NeedByOla));
    }

    [Fact]
    public void OtherUser_CannotModify() =>
        Assert.False(ResourcePermissions.CanModify(User("kari-id", AppRoles.User), OwnedByOla));

    [Fact]
    public void Admin_CanModifyEverything()
    {
        var admin = User("admin-id", AppRoles.Admin);
        Assert.True(ResourcePermissions.CanModify(admin, OwnedByOla));
        Assert.True(ResourcePermissions.CanModify(admin, SeedData));
    }

    [Fact]
    public void NormalUser_CannotModifyResourceWithoutOwner() =>
        Assert.False(ResourcePermissions.CanModify(User("ola-id"), SeedData));

    [Fact]
    public void AnonymousUser_CannotModify()
    {
        Assert.False(ResourcePermissions.CanModify(null, OwnedByOla));
        Assert.False(ResourcePermissions.CanModify(new ClaimsPrincipal(new ClaimsIdentity()), OwnedByOla));
    }
}
