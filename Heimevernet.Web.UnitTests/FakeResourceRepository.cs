using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Controller tests supply known records without needing a running database.
internal sealed class FakeResourceRepository(params Resource[] resources) : IResourceRepository
{
    private readonly List<Resource> _resources = [.. resources];

    public List<Resource> Added { get; } = [];
    public List<Resource> Updated { get; } = [];
    public List<int> Deleted { get; } = [];

    public IReadOnlyCollection<Resource> GetAll() => _resources;

    public Resource? GetById(int id) => _resources.SingleOrDefault(resource => resource.Id == id);

    public int Add(Resource resource)
    {
        var id = _resources.Count == 0 ? 1 : _resources.Max(existing => existing.Id) + 1;
        var saved = Copy(resource, id);
        _resources.Add(saved);
        Added.Add(saved);
        return id;
    }

    public bool Update(Resource resource)
    {
        var index = _resources.FindIndex(existing => existing.Id == resource.Id);
        if (index < 0)
        {
            return false;
        }
        _resources[index] = Copy(resource, resource.Id);
        Updated.Add(_resources[index]);
        return true;
    }

    public bool Delete(int id)
    {
        Deleted.Add(id);
        return _resources.RemoveAll(resource => resource.Id == id) == 1;
    }

    private static Resource Copy(Resource resource, int id) => new()
    {
        Id = id,
        Name = resource.Name,
        Description = resource.Description,
        Type = resource.Type,
        Address = resource.Address,
        City = resource.City,
        ZipCode = resource.ZipCode,
        OwnerTelephoneNumber = resource.OwnerTelephoneNumber,
        CreatedBy = resource.CreatedBy
    };
}
