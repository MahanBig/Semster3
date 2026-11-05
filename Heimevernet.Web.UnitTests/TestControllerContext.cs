using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Heimevernet.Web.UnitTests;

// Gir en controller det den ville fått fra en ekte forespørsel: en innlogget bruker og TempData.
internal static class TestControllerContext
{
    public static T WithUser<T>(this T controller, string userName = "ola@test.no", params string[] roles) where T : Controller
    {
        // Testene bruker e-posten som bruker-Id også.
        var claims = new List<Claim> { new(ClaimTypes.Name, userName), new(ClaimTypes.NameIdentifier, userName) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
        return controller;
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _values = new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _values;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _values = values;
    }
}
