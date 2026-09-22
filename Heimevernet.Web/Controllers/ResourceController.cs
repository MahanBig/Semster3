using Microsoft.AspNetCore.Mvc;

using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Controllers
{
    public class ResourceController : Controller
    {
        private readonly IResourceRepository _resourceRepository;

        public ResourceController(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        [HttpGet]
        public IActionResult Index(int? id = null)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index", "ResourceHandler");
            }

            var resource = _resourceRepository.GetById(id.Value);
            if (resource is null)
            {
                return NotFound();
            }

            var viewModel = new ResourceViewModel
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
