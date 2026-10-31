using Heimevernet.Web.Models.Entities;
using Heimevernet.Web.Models.ViewModels.Resource;

namespace Heimevernet.Web.Models.ViewModels;

// Alt forsiden (kartet) trenger: ressursene til listen og posisjonene til kartet.
public class MapPageViewModel
{
    public IReadOnlyList<ResourceViewModel> Resources { get; init; } = [];
    public IReadOnlyCollection<Position> Positions { get; init; } = [];
}
