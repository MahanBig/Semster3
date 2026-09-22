using Heimevernet.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.DataAccess;

public class EfResourceRepository(HeimevernetDbContext dbContext) : IResourceRepository
{
    public IReadOnlyCollection<Resource> GetAll()
    {
        // These pages only display records, so EF does not need to track changes.
        return dbContext.Resources
            .AsNoTracking()
            .OrderBy(resource => resource.Id)
            .ToList()
            .AsReadOnly();
    }

    public Resource? GetById(int id)
    {
        return dbContext.Resources
            .AsNoTracking()
            .SingleOrDefault(resource => resource.Id == id);
    }
}
