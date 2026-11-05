using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Leser kategoriene fra tabellen Categories.
public class DapperCategoryRepository(IDbConnectionFactory connectionFactory) : ICategoryRepository
{
    public IReadOnlyList<Category> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Category>("SELECT Id, Name, Description FROM Categories ORDER BY Id").ToList();
    }

    public Category? GetByName(string name)
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.QuerySingleOrDefault<Category>(
            "SELECT Id, Name, Description FROM Categories WHERE Name = @Name", new { Name = name });
    }
}
