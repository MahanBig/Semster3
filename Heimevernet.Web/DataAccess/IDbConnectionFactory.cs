using System.Data.Common;

namespace Heimevernet.Web.DataAccess;

// Lager nye databasetilkoblinger. Klassene som bruker databasen spør denne om en tilkobling
// i stedet for å lage en MySqlConnection selv. Da kan testene gi dem en SQLite-tilkobling i stedet.
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
