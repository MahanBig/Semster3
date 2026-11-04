using Heimevernet.Web.Authorization;
using Heimevernet.Web.DataAccess;
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

        public ResourceHandlerController(IResourceRepository resourceRepository) // Repository kommer inn via dependency injection. Det er registrert i Program.cs.
        {
            _resourceRepository = resourceRepository;
        }

        [HttpGet] // Henter alle ressurser fra databasen og sender dem til viewet som en liste.
        public IActionResult Index()
        {
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

        // ---------- Legge til ----------

        [HttpGet]
        [Authorize] // Alle innloggede brukere kan legge til ressurser.
        public IActionResult Create()
        {
            return View(new ResourceViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ResourceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Viser skjemaet igjen med feilmeldingene.
            }

            var resource = ToEntity(model, id: 0);
            resource.CreatedBy = User.Identity?.Name; // Den innloggede brukeren blir eier av ressursen.
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

            return View(ToViewModel(resource));
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

            if (!ModelState.IsValid)
            {
                model.Id = id;
                return View(model);
            }

            var updated = ToEntity(model, id);
            updated.CreatedBy = existing.CreatedBy; // Eieren endres ikke.
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

            return View(ToViewModel(resource)); // Viser en side der brukeren må bekrefte slettingen.
        }

        // POST /ResourceHandler/Delete/5. Egen metodenavn fordi GET og POST har samme parametere.
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

        private static Resource ToEntity(ResourceViewModel model, int id) => new()
        {
            Id = id,
            Name = model.Name.Trim(),
            Description = model.Description.Trim(),
            Type = model.Type.Trim(),
            Address = EmptyToNull(model.Address),
            City = EmptyToNull(model.City),
            ZipCode = EmptyToNull(model.ZipCode),
            OwnerTelephoneNumber = EmptyToNull(model.OwnerTelephoneNumber)
        };

        private static ResourceViewModel ToViewModel(Resource resource) => new()
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Type = resource.Type,
            Address = resource.Address,
            City = resource.City,
            ZipCode = resource.ZipCode,
            OwnerTelephoneNumber = resource.OwnerTelephoneNumber,
            CreatedBy = resource.CreatedBy
        };

        private static string? EmptyToNull(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
