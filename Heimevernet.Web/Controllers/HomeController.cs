using System.Diagnostics;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

public class HomeController(IResourceRepository resourceRepository) : Controller
{
    // Forsiden: kart og liste over ressurser.
    public IActionResult Index()
    {
        var model = new MapPageViewModel
        {
            Resources = resourceRepository.GetAll()
                .Select(Models.ViewModels.Resource.ResourceViewModel.FromEntity)
                .ToList()
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous] // Feilsiden må kunne vises for alle.
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
