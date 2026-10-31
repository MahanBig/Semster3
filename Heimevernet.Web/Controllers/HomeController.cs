using System.Diagnostics;
using System.Globalization;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers;

public class HomeController(IPositionRepository positionRepository, IResourceRepository resourceRepository) : Controller
{
    // Forsiden: kart med registrerte posisjoner og liste over ressurser.
    public IActionResult Index()
    {
        var model = new MapPageViewModel
        {
            Positions = positionRepository.GetAll(),
            Resources = resourceRepository.GetAll()
                .Select(resource => new Models.ViewModels.Resource.ResourceViewModel
                {
                    Id = resource.Id,
                    Name = resource.Name,
                    Description = resource.Description,
                    Type = resource.Type
                })
                .ToList()
        };
        return View(model);
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
        return View(new PositionModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken] // Stopper at en annen nettside sender inn skjemaet på vegne av brukeren (CSRF).
    public IActionResult CorrectMap(PositionModel model)
    {
        // Tekst -> tall. InvariantCulture betyr punktum som desimaltegn, uansett språk på serveren.
        var latitude = ParseCoordinate(model.Latitude);
        var longitude = ParseCoordinate(model.Longitude);
        if (latitude is null or < -90 or > 90)
        {
            ModelState.AddModelError(nameof(model.Latitude), "Breddegraden må være mellom -90 og 90.");
        }
        if (longitude is null or < -180 or > 180)
        {
            ModelState.AddModelError(nameof(model.Longitude), "Lengdegraden må være mellom -180 og 180.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        positionRepository.Add(new Position
        {
            Latitude = latitude!.Value,
            Longitude = longitude!.Value,
            Description = model.Description.Trim(),
            CreatedBy = User?.Identity?.Name,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Post/Redirect/Get: etter lagring sender vi nettleseren til oversikten.
        // Da lagres ikke posisjonen to ganger hvis brukeren trykker F5.
        return RedirectToAction(nameof(CorrectionOverview));
    }

    [HttpGet]
    public IActionResult CorrectionOverview()
    {
        return View(positionRepository.GetAll());
    }

    private static double? ParseCoordinate(string? value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }
}
