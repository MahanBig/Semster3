using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Bildene til ressursene (tabellen ResourceImages). En ressurs har høyst ett bilde.
public interface IResourceImageRepository
{
    ResourceImage? Get(int resourceId); // Returnerer null hvis ressursen ikke har bilde.
    void Save(int resourceId, string contentType, byte[] data); // Erstatter et bilde som finnes fra før.
    bool Delete(int resourceId); // Returnerer false hvis det ikke fantes et bilde.
}
