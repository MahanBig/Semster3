using Microsoft.AspNetCore.Mvc;

using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Controllers
{
    public class ResourceController : Controller // Controller for detaljsiden til én ressurs
    {
        private readonly IResourceRepository _resourceRepository;

        public ResourceController(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        [HttpGet] 
        public IActionResult Index(int? id = null)
        {
            if (!id.HasValue) // Ingen id i URL-en: send brukeren tilbake til ressurslisten.
            {
                return RedirectToAction("Index", "ResourceHandler");
            }

            var resource = _resourceRepository.GetById(id.Value);
            if (resource is null)
            {
                return NotFound();
            }

            var viewModel = new ResourceViewModel  // Vi mapper entiteten til en view model, så viewet bare får feltene det trenger.
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Description,
                Type = resource.Type
            };

            return View(viewModel);
        }
    }
}
