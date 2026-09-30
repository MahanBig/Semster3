using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public static class ResourceDbSeeder 
{
    public static void Seed(HeimevernetDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        // Preserve existing records and edits when the application restarts.
        if (dbContext.Resources.Any())
        {
            return;
        }

        dbContext.Resources.AddRange(
            new Resource { Name = "Resource 1", Description = "Db Description 1", Type = "Type A" },
            new Resource { Name = "Resource 2", Description = "Db Description 2", Type = "Type B" },
            new Resource { Name = "Resource 3", Description = "Db Description 3", Type = "Type C" });

        // MariaDB generates each record's Id when the changes are saved.
        dbContext.SaveChanges();
    }
}
