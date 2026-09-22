using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Controller tests supply known records without needing a running database.
internal sealed class FakeResourceRepository(params Resource[] resources) : IResourceRepository
{
    public IReadOnlyCollection<Resource> GetAll() => resources;

    public Resource? GetById(int id) => resources.SingleOrDefault(resource => resource.Id == id);
}
