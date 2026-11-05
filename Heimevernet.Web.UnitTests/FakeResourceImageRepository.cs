using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Falsk bildelager i minnet for controller-testene.
internal sealed class FakeResourceImageRepository : IResourceImageRepository
{
    public Dictionary<int, ResourceImage> Images { get; } = [];

    public ResourceImage? Get(int resourceId) => Images.GetValueOrDefault(resourceId);

    public void Save(int resourceId, string contentType, byte[] data) =>
        Images[resourceId] = new ResourceImage { ResourceId = resourceId, ContentType = contentType, Data = data };

    public bool Delete(int resourceId) => Images.Remove(resourceId);
}
