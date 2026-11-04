using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

public interface ICategoryRepository
{
    IReadOnlyList<Category> GetAll();
    Category? GetByName(string name);
}
