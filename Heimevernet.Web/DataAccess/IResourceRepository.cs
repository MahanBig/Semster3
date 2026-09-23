using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface IResourceRepository
{
    IReadOnlyCollection<Resource> GetAll();
    Resource? GetById(int id);
}
