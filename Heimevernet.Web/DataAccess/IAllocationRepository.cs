using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Kontrakt for tildelinger. Den ekte versjonen er DapperAllocationRepository.
public interface IAllocationRepository
{
    IReadOnlyList<Allocation> GetForNeed(int needId);

    // Returnerer false hvis ressursen allerede er tildelt dette behovet.
    bool Allocate(int needId, int resourceId, int quantity);

    // Returnerer false hvis tildelingen ikke fantes.
    bool Remove(int needId, int resourceId);
}
