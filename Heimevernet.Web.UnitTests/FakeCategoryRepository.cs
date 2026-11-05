using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

// Falsk kategorirepository for controller-testene. Samme kategorier og Id-er som i SqliteTestDatabase.
internal sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly List<Category> _categories = ResourceCategory.All
        .Select((category, index) => new Category { Id = index + 1, Name = category.Label })
        .ToList();

    public IReadOnlyList<Category> GetAll() => _categories;

    public Category? GetByName(string name) => _categories.SingleOrDefault(category => category.Name == name);
}
