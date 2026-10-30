using System.Data.Common;
using MySqlConnector;

namespace Heimevernet.Web.DataAccess;

// Den ekte versjonen som brukes i appen: kobler til MariaDB.
// Hver kall gir en ny tilkobling. Den lukkes igjen med "using", og MySqlConnector gjenbruker
// tilkoblingene bak kulissene (connection pooling), så dette er raskt.
public class MySqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new MySqlConnection(connectionString);
}
