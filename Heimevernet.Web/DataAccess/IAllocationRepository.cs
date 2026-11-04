using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface IAllocationRepository
{
    IReadOnlyList<Allocation> GetForNeed(int needId);

    // Returnerer false hvis ressursen allerede er tildelt dette behovet.
    bool Allocate(int needId, int resourceId, int quantity);

    bool Remove(int needId, int resourceId);
}
