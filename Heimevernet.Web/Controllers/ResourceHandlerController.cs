using Heimevernet.Web.Models.ViewModels.Resource;
using Microsoft.AspNetCore.Mvc;
using Heimevernet.Web.DataAccess;

namespace Heimevernet.Web.Controllers
{
    public class ResourceHandlerController : Controller
    {
        private readonly IResourceRepository _resourceRepository;

        public ResourceHandlerController(IResourceRepository resourceRepository)
        {
            _resourceRepository = resourceRepository;
        }

        [HttpGet]
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

        [HttpPost]
        public ActionResult Create(ResourceViewModel model) 
        { 
            //oppdater database med nye data og returner resultat
            return View(model);
        }
    }
}
