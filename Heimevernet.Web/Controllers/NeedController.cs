using System.Security.Claims;
using Heimevernet.Web.Authorization;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Need;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Heimevernet.Web.Controllers;

// Krever innlogging via FallbackPolicy i Program.cs.
public class NeedController(
    INeedRepository needRepository,
    IResourceRepository resourceRepository,
    IAllocationRepository allocationRepository,
    ICategoryRepository categoryRepository,
    IOrganizationRepository organizationRepository) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = needRepository.GetAll().Select(NeedViewModel.FromEntity).ToList();
        return View(model);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        var need = needRepository.GetById(id);
        if (need is null)
        {
            return NotFound();
        }

        var viewModel = NeedViewModel.FromEntity(need);
        viewModel.CanModify = ResourcePermissions.CanModify(User, need);

        var allocations = allocationRepository.GetForNeed(id);
        var allocatedResourceIds = allocations.Select(allocation => allocation.ResourceId).ToHashSet();

        // Ledige ressurser brukeren eier (admin: alle), samme kategori først.
        var available = resourceRepository.GetAll()
            .Where(resource => resource.Status == ResourceStatus.Available)
            .Where(resource => !allocatedResourceIds.Contains(resource.Id))
            .Where(resource => ResourcePermissions.CanModify(User, resource))
            .OrderBy(resource => resource.CategoryId == need.CategoryId ? 0 : 1)
            .ThenBy(resource => resource.Name)
            .ToList();

        return View(new NeedDetailsViewModel
        {
            Need = viewModel,
            Allocations = allocations
                .Select(allocation => new AllocationRow
                {
                    Allocation = allocation,
                    CanRemove = viewModel.CanModify
                        || ResourcePermissions.IsOwnerOrAdmin(User, allocation.ResourceOwnerId)
                })
                .ToList(),
            AvailableResources = available
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        SetOrganizationList(null);
        return View(new NeedViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(NeedViewModel model)
    {
        var category = FindCategory(model);
        ValidateOrganization(model);
        if (!ModelState.IsValid || category is null)
        {
            SetOrganizationList(model.OrganizationId);
            return View(model);
        }

        var need = ToEntity(model, id: 0, category.Id);
        need.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        need.RegisteredAt = DateTime.UtcNow;
        need.Status = NeedStatus.Open;
        var id = needRepository.Add(need);

        TempData["Message"] = "Behovet ble lagret.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Edit(int id)
    {
        var need = needRepository.GetById(id);
        if (need is null)
        {
            return NotFound();
        }
        if (!ResourcePermissions.CanModify(User, need))
        {
            return Forbid();
        }

        SetOrganizationList(need.OrganizationId);
        return View(NeedViewModel.FromEntity(need));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, NeedViewModel model)
    {
        // Sjekker tilgang på POST også, ikke bare på GET.
        var existing = needRepository.GetById(id);
        if (existing is null)
        {
            return NotFound();
        }
        if (!ResourcePermissions.CanModify(User, existing))
        {
            return Forbid();
        }

        var category = FindCategory(model);
        ValidateOrganization(model);
        if (!ModelState.IsValid || category is null)
        {
            model.Id = id;
            SetOrganizationList(model.OrganizationId);
            return View(model);
        }

        needRepository.Update(ToEntity(model, id, category.Id));

        TempData["Message"] = "Endringene ble lagret.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public IActionResult Delete(int id)
    {
        var need = needRepository.GetById(id);
        if (need is null)
        {
            return NotFound();
        }
        if (!ResourcePermissions.CanModify(User, need))
        {
            return Forbid();
        }

        return View(NeedViewModel.FromEntity(need));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var need = needRepository.GetById(id);
        if (need is null)
        {
            return NotFound();
        }
        if (!ResourcePermissions.CanModify(User, need))
        {
            return Forbid();
        }

        needRepository.Delete(id);

        TempData["Message"] = $"Behovet «{need.Title}» ble slettet.";
        return RedirectToAction(nameof(Index));
    }

    // Bare eieren av ressursen (eller admin) kan tildele den.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Allocate(int id, int resourceId, int quantity)
    {
        var need = needRepository.GetById(id);
        var resource = resourceRepository.GetById(resourceId);
        if (need is null || resource is null)
        {
            return NotFound();
        }
        if (!ResourcePermissions.CanModify(User, resource))
        {
            return Forbid();
        }

        if (resource.Status != ResourceStatus.Available)
        {
            TempData["Error"] = $"«{resource.Name}» er ikke tilgjengelig.";
        }
        else if (quantity < 1 || quantity > resource.Quantity)
        {
            TempData["Error"] = $"Antallet må være mellom 1 og {resource.Quantity}.";
        }
        else if (!allocationRepository.Allocate(id, resourceId, quantity))
        {
            TempData["Error"] = $"«{resource.Name}» er allerede tildelt dette behovet.";
        }
        else
        {
            TempData["Message"] = $"«{resource.Name}» ble tildelt behovet.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveAllocation(int id, int resourceId)
    {
        var need = needRepository.GetById(id);
        var resource = resourceRepository.GetById(resourceId);
        if (need is null || resource is null)
        {
            return NotFound();
        }

        if (!ResourcePermissions.CanModify(User, need) && !ResourcePermissions.CanModify(User, resource))
        {
            return Forbid();
        }

        allocationRepository.Remove(id, resourceId);
        TempData["Message"] = $"Tildelingen av «{resource.Name}» ble fjernet.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Category? FindCategory(NeedViewModel model)
    {
        var category = string.IsNullOrWhiteSpace(model.Category) ? null : categoryRepository.GetByName(model.Category.Trim());
        if (category is null && !string.IsNullOrWhiteSpace(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Ukjent kategori.");
        }
        return category;
    }

    // Uten denne sjekken gir en ukjent OrganizationId en databasefeil.
    private void ValidateOrganization(NeedViewModel model)
    {
        if (model.OrganizationId is { } organizationId
            && organizationRepository.GetAll().All(organization => organization.Id != organizationId))
        {
            ModelState.AddModelError(nameof(model.OrganizationId), "Ukjent organisasjon.");
        }
    }

    private void SetOrganizationList(int? selectedId)
    {
        ViewData["Organizations"] = new SelectList(organizationRepository.GetAll(), "Id", "Name", selectedId);
    }

    private static Need ToEntity(NeedViewModel model, int id, int categoryId) => new()
    {
        Id = id,
        Title = model.Title.Trim(),
        Description = model.Description.Trim(),
        Quantity = model.Quantity,
        Priority = model.Priority,
        Deadline = model.Deadline,
        OrganizationId = model.OrganizationId,
        CategoryId = categoryId,
        Address = EmptyToNull(model.Address),
        PostalCode = EmptyToNull(model.PostalCode),
        PostalPlace = EmptyToNull(model.PostalPlace)
    };

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
