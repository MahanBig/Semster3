using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Kontrakt for å lagre og hente kartposisjoner. Gjør at HomeController kan testes med en falsk versjon.
public interface IPositionRepository
{
    IReadOnlyCollection<Position> GetAll();
    int Add(Position position); // Returnerer Id-en databasen ga den nye raden.
}
