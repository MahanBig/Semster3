using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Controller tests supply known records without needing a running database.
internal sealed class FakeResourceRepository(params Resource[] resources) : IResourceRepository
{
    private readonly List<Resource> _resources = [.. resources];

    public List<Resource> Added { get; } = [];

    public IReadOnlyCollection<Resource> GetAll() => _resources;

    public Resource? GetById(int id) => _resources.SingleOrDefault(resource => resource.Id == id);

    public int Add(Resource resource)
    {
        var id = _resources.Count == 0 ? 1 : _resources.Max(existing => existing.Id) + 1;
        var saved = new Resource
        {
            Id = id,
            Name = resource.Name,
            Description = resource.Description,
            Type = resource.Type,
            Address = resource.Address,
            City = resource.City,
            ZipCode = resource.ZipCode,
            OwnerTelephoneNumber = resource.OwnerTelephoneNumber
        };
        _resources.Add(saved);
        Added.Add(saved);
        return id;
    }
}
