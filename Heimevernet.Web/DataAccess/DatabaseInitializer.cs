using Dapper;

namespace Heimevernet.Web.DataAccess;

// Erstatter EF-migrasjonene: kjører Schema.sql ved oppstart, så tabellene finnes.
public static class DatabaseInitializer
{
    private const string SchemaResourceName = "Heimevernet.Web.Schema.sql"; // Se <EmbeddedResource> i .csproj.

    public static void EnsureSchema(IDbConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        using var connection = connectionFactory.CreateConnection();
        StopIfDatabaseIsFromOldVersion(connection);
        connection.Execute(ReadSchemaScript()); // Dapper åpner tilkoblingen selv hvis den er lukket.
    }

    // Schema.sql endrer ikke tabeller som finnes. En gammel database ville gitt en uklar SQL-feil senere.
    private static void StopIfDatabaseIsFromOldVersion(System.Data.Common.DbConnection connection)
    {
        var hasOldResourcesTable = connection.ExecuteScalar<int>(
            """
            SELECT COUNT(*) FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Resources' AND COLUMN_NAME = 'Type'
            """) > 0;

        if (hasOldResourcesTable)
        {
            throw new InvalidOperationException(
                "The database was created by an older version of the app (Resources still has the column 'Type'). " +
                "Delete it and start again with: docker compose down -v, then docker compose up --build. " +
                "(Databasen er fra en eldre versjon. Slett den med 'docker compose down -v' og start på nytt.)");
        }
    }

    private static string ReadSchemaScript()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SchemaResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
