using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers
{
    public class ResourceHandlerController : Controller // Controller for ressurslisten (/ResourceHandler).
    {
        private readonly IResourceRepository _resourceRepository;

        public ResourceHandlerController(IResourceRepository resourceRepository) // Repository kommer inn via dependency injection. Det er registrert i Program.cs.
        {
            _resourceRepository = resourceRepository;
        }

        [HttpGet] // Henter alle ressurser fra databasen og sender dem til viewet som en liste.
        public IActionResult Index()
        {
            // Give the view the fields it needs for displaying each database row.
            var model = _resourceRepository.GetAll()
                .Select(resource => new ResourceViewModel
                {
                    Id = resource.Id,
                    Name = resource.Name,
                    Description = resource.Description,
                    Type = resource.Type
                })
                .ToList();

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)] // Bare admin kan legge til ressurser.
        public IActionResult Create()
        {
            return View(new ResourceViewModel());
        }

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ResourceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Viser skjemaet igjen med feilmeldingene.
            }

            var id = _resourceRepository.Add(new Resource
            {
                Name = model.Name.Trim(),
                Description = model.Description.Trim(),
                Type = model.Type.Trim(),
                Address = EmptyToNull(model.Address),
                City = EmptyToNull(model.City),
                ZipCode = EmptyToNull(model.ZipCode),
                OwnerTelephoneNumber = EmptyToNull(model.OwnerTelephoneNumber)
            });

            // TempData overlever én redirect, så detaljsiden kan vise en bekreftelse.
            TempData["Message"] = "Ressursen ble lagret.";
            return RedirectToAction("Index", "Resource", new { id });
        }

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
