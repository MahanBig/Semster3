using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Web.DataAccess;

public class HeimevernetDbContext(DbContextOptions<HeimevernetDbContext> options) : DbContext(options)
{
    // Add DbSet<TEntity> properties here when database entity classes are introduced.
}
