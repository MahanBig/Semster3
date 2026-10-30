using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.ViewModels;

namespace Heimevernet.Web.Controllers;

public class HomeController : Controller  
{
    private readonly ILogger<HomeController> _logger;
    private static List<PositionModel> positions = new List<PositionModel>(); 
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet]
    [Authorize] // Må være logget inn for å registrere en posisjon.
    public IActionResult CorrectMap() 
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    public IActionResult CorrectMap(PositionModel model) 
    {
        if (ModelState.IsValid) 
        {
            positions.Add(model);
            return View("CorrectionOverview", positions); 
        }
        return View();
    }

    [HttpGet]
    public IActionResult CorrectionOverview()
    {
        return View(positions);
    }
}
