using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Models.ViewModels;

// Ressursene til listen på forsiden.
public class MapPageViewModel
{
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];
}
