using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Leser organisasjonene fra tabellen Organizations, sortert på navn.
public class DapperOrganizationRepository(IDbConnectionFactory connectionFactory) : IOrganizationRepository
{
    public IReadOnlyList<Organization> GetAll()
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Query<Organization>("SELECT Id, Name, Type FROM Organizations ORDER BY Name").ToList();
    }
}
