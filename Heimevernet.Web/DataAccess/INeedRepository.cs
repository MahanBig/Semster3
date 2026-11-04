using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface INeedRepository
{
    IReadOnlyList<Need> GetAll();
    Need? GetById(int id);
    int Add(Need need); // Returnerer den nye Id-en.
    bool Update(Need need);
    bool Delete(int id);
}
