using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.Models.ViewModels.Need;

// Alt detaljsiden for ett behov trenger: selve behovet, tildelingene og ressursene som kan tildeles.
public class NeedDetailsViewModel
{
    public NeedViewModel Need { get; init; } = new();

    public IReadOnlyList<AllocationRow> Allocations { get; init; } = [];

    // Ledige ressurser den innloggede brukeren kan tildele.
    public IReadOnlyList<Entities.Resource> AvailableResources { get; init; } = [];

    public bool IsCovered => Need.Status == NeedStatus.Covered;
}

// Én tildeling i listen. CanRemove avgjør om Fjern-knappen vises (regnes ut i NeedController.Details).
public class AllocationRow
{
    public Allocation Allocation { get; init; } = new();
    public bool CanRemove { get; init; }
}
