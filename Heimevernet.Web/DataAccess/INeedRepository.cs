using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Kontrakt for behov. Controlleren kjenner bare interfacet, så den kan testes uten database.
public interface INeedRepository
{
    IReadOnlyList<Need> GetAll();
    Need? GetById(int id); // Returnerer null hvis behovet ikke finnes.
    int Add(Need need); // Returnerer den nye Id-en.
    bool Update(Need need); // Returnerer false hvis behovet ikke finnes.
    bool Delete(int id); // Returnerer false hvis behovet ikke finnes.
}
