using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

internal sealed class FakePositionRepository : IPositionRepository
{
    public List<Position> Added { get; } = [];

    public IReadOnlyCollection<Position> GetAll() => Added;

    public int Add(Position position)
    {
        Added.Add(position);
        return Added.Count;
    }
}
