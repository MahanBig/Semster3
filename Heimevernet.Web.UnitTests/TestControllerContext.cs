using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Heimevernet.Web.UnitTests;

// Gives a controller what it would get from a real request: a logged-in user and TempData.
internal static class TestControllerContext
{
    public static T WithUser<T>(this T controller, string userName = "ola@test.no") where T : Controller
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, userName)], authenticationType: "Test");
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
