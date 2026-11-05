using Heimevernet.Web.Authorization;
using Heimevernet.Web.DataAccess;
using System.Security.Claims;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heimevernet.Web.Controllers
{
    // Ressurslisten (/ResourceHandler) og alt som endrer ressurser: legge til, endre og slette.
    // Regelen for hvem som får endre og slette ligger i ResourcePermissions.
    public class ResourceHandlerController : Controller
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly ICategoryRepository _categoryRepository;

        // Repositoryene kommer inn via dependency injection. De er registrert i Program.cs.
        public ResourceHandlerController(IResourceRepository resourceRepository, ICategoryRepository categoryRepository)
        {
            _resourceRepository = resourceRepository;
            _categoryRepository = categoryRepository;
        }

        [HttpGet] // Henter alle ressurser fra databasen og sender dem til viewet som en liste.
        public IActionResult Index()
        {
            var model = _resourceRepository.GetAll()
                .Select(ResourceViewModel.FromEntity)
                .ToList();

            return View(model);
        }

        // ---------- Legge til ----------

        // Kommer brukeren fra kartet ("Legg til ressurs her"), er punktet allerede valgt.
        [HttpGet]
        [Authorize] // Alle innloggede brukere kan legge til ressurser.
        public IActionResult Create(double? lat = null, double? lng = null)
        {
            var model = new ResourceViewModel();
            if (lat is >= -90 and <= 90 && lng is >= -180 and <= 180)
            {
                model.Latitude = Math.Round(lat.Value, 6);
                model.Longitude = Math.Round(lng.Value, 6);
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ResourceViewModel model)
        {
            var category = FindCategory(model);
            if (!ModelState.IsValid || category is null)
            {
                return View(model); // Viser skjemaet igjen med feilmeldingene.
            }

            var resource = ToEntity(model, id: 0, category.Id);
            resource.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Den innloggede brukeren blir eier.
            resource.RegisteredAt = DateTime.UtcNow;
            var id = _resourceRepository.Add(resource);

            // TempData overlever én redirect, så detaljsiden kan vise en bekreftelse.
            TempData["Message"] = "Ressursen ble lagret.";
            return RedirectToAction("Index", "Resource", new { id });
        }

        // ---------- Endre ----------

        [HttpGet]
        [Authorize]
        public IActionResult Edit(int id)
        {
            var resource = _resourceRepository.GetById(id);
            if (resource is null)
            {
                return NotFound();
            }
            if (!ResourcePermissions.CanModify(User, resource))
            {
                return Forbid(); // Logget inn, men ikke eier eller admin: sendes til "Ingen tilgang".
            }

            return View(ResourceViewModel.FromEntity(resource));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ResourceViewModel model)
        {
            // Vi henter ressursen fra databasen på nytt og sjekker tilgang her også.
            // Det holder ikke å skjule knappen: noen kan sende POST-forespørselen direkte.
            var existing = _resourceRepository.GetById(id);
            if (existing is null)
            {
                return NotFound();
            }
            if (!ResourcePermissions.CanModify(User, existing))
            {
                return Forbid();
            }

            var category = FindCategory(model);
            if (!ModelState.IsValid || category is null)
            {
                model.Id = id;
                return View(model);
            }

            var updated = ToEntity(model, id, category.Id);
            updated.UserId = existing.UserId; // Eieren endres ikke.
            _resourceRepository.Update(updated);

            TempData["Message"] = "Endringene ble lagret.";
            return RedirectToAction("Index", "Resource", new { id });
        }

        // ---------- Slette ----------

        [HttpGet]
        [Authorize]
        public IActionResult Delete(int id)
        {
            var resource = _resourceRepository.GetById(id);
            if (resource is null)
            {
                return NotFound();
            }
            if (!ResourcePermissions.CanModify(User, resource))
            {
                return Forbid();
            }

            return View(ResourceViewModel.FromEntity(resource)); // Viser en side der brukeren må bekrefte slettingen.
        }

        // POST /ResourceHandler/Delete/5. Eget metodenavn fordi GET og POST har samme parametere.
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var resource = _resourceRepository.GetById(id);
            if (resource is null)
            {
                return NotFound();
            }
            if (!ResourcePermissions.CanModify(User, resource))
            {
                return Forbid();
            }

            _resourceRepository.Delete(id);

            TempData["Message"] = $"Ressursen «{resource.Name}» ble slettet.";
            return RedirectToAction(nameof(Index));
        }

        // ---------- Hjelpemetoder ----------

        // Skjemaet sender kategorinavnet. Databasen trenger Id-en.
        private Category? FindCategory(ResourceViewModel model)
        {
            var category = string.IsNullOrWhiteSpace(model.Category) ? null : _categoryRepository.GetByName(model.Category.Trim());
            if (category is null && !string.IsNullOrWhiteSpace(model.Category))
            {
                ModelState.AddModelError(nameof(model.Category), "Ukjent kategori.");
            }
            return category;
        }

        private static Resource ToEntity(ResourceViewModel model, int id, int categoryId) => new()
        {
            Id = id,
            Name = model.Name.Trim(),
            Description = model.Description.Trim(),
            Quantity = model.Quantity,
            Status = model.Status,
            CategoryId = categoryId,
            Address = EmptyToNull(model.Address),
            PostalCode = EmptyToNull(model.PostalCode),
            PostalPlace = EmptyToNull(model.PostalPlace),
            // DECIMAL(9,6) i databasen: 6 desimaler er ca. 10 cm, mer enn nok.
            Latitude = model.Latitude is { } lat ? Math.Round(lat, 6) : null,
            Longitude = model.Longitude is { } lng ? Math.Round(lng, 6) : null
        };

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
