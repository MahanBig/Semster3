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
        connection.Execute(ReadSchemaScript()); // Dapper åpner tilkoblingen selv hvis den er lukket.
    }

    private static string ReadSchemaScript()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{SchemaResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
