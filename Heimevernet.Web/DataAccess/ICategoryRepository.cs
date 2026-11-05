using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Kategoriene (Mat, Vann osv.). De legges inn av Schema.sql og endres ikke i appen.
public interface ICategoryRepository
{
    IReadOnlyList<Category> GetAll();
    Category? GetByName(string name); // Returnerer null hvis navnet ikke finnes.
}
