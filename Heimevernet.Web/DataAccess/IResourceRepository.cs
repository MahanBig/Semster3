using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface IResourceRepository // Kontrakt for å hente ressurser. Gjør at controllerne kan testes med en falsk versjon.
{
    IReadOnlyCollection<Resource> GetAll();
    Resource? GetById(int id);
    int Add(Resource resource); // Returnerer Id-en databasen ga den nye raden.
}
