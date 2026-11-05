using Microsoft.AspNetCore.Mvc;

using Heimevernet.Web.Authorization;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Controllers
{
    public class ResourceController : Controller // Controller for detaljsiden til én ressurs
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly IResourceImageRepository _imageRepository;

        public ResourceController(IResourceRepository resourceRepository, IResourceImageRepository imageRepository)
        {
            _resourceRepository = resourceRepository;
            _imageRepository = imageRepository;
        }

        // GET /Resource/Image/5: bildet til ressursen. Samme tilgang som detaljsiden:
        // en Privat-bruker får ikke andres bilder ved å gjette adressen.
        [HttpGet]
        public IActionResult Image(int id)
        {
            var resource = _resourceRepository.GetById(id);
            var image = resource is not null && ResourcePermissions.CanView(User, resource) ? _imageRepository.Get(id) : null;
            if (image is null)
            {
                return NotFound();
            }

            // "private": bare nettleseren til brukeren kan mellomlagre bildet, ikke delte proxyer.
            Response.Headers.CacheControl = "private, max-age=300";
            return File(image.Data, image.ContentType);
        }

        [HttpGet] 
        public IActionResult Index(int? id = null)
        {
            if (!id.HasValue) // Ingen id i URL-en: send brukeren tilbake til ressurslisten.
            {
                return RedirectToAction("Index", "ResourceHandler");
            }

            var resource = _resourceRepository.GetById(id.Value);
            // En Privat-bruker som prøver å åpne en annens ressurs, får 404 og ikke "Ingen tilgang",
            // så det ikke avsløres at ressursen finnes.
            if (resource is null || !ResourcePermissions.CanView(User, resource))
            {
                return NotFound();
            }

            // Vi mapper entiteten til en view model, så viewet bare får feltene det trenger.
            var viewModel = ResourceViewModel.FromEntity(resource);
            viewModel.CanModify = ResourcePermissions.CanModify(User, resource); // Viser Endre/Slett bare for eier og admin.

            return View(viewModel);
        }
    }
}
