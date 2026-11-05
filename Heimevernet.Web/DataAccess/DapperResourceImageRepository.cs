using Dapper;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.DataAccess;

// Leser og lagrer bilder i ResourceImages med Dapper. Bildedataene er en byte[] som lagres som MEDIUMBLOB.
public class DapperResourceImageRepository(IDbConnectionFactory connectionFactory) : IResourceImageRepository
{
    public ResourceImage? Get(int resourceId)
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.QuerySingleOrDefault<ResourceImage>(
            "SELECT ResourceId, ContentType, Data, UploadedAt FROM ResourceImages WHERE ResourceId = @ResourceId",
            new { ResourceId = resourceId });
    }

    public void Save(int resourceId, string contentType, byte[] data)
    {
        using var connection = connectionFactory.CreateConnection();
        connection.Open();

        // Slett det gamle og legg inn det nye i én transaksjon. Det virker likt i MariaDB og i SQLite (testene),
        // i motsetning til "INSERT ... ON DUPLICATE KEY UPDATE", som bare finnes i MariaDB/MySQL.
        using var transaction = connection.BeginTransaction();
        connection.Execute("DELETE FROM ResourceImages WHERE ResourceId = @ResourceId", new { ResourceId = resourceId }, transaction);
        connection.Execute(
            """
            INSERT INTO ResourceImages (ResourceId, ContentType, Data, UploadedAt)
            VALUES (@ResourceId, @ContentType, @Data, @UploadedAt)
            """,
            new { ResourceId = resourceId, ContentType = contentType, Data = data, UploadedAt = DateTime.UtcNow },
            transaction);
        transaction.Commit();
    }

    public bool Delete(int resourceId)
    {
        using var connection = connectionFactory.CreateConnection();
        return connection.Execute("DELETE FROM ResourceImages WHERE ResourceId = @ResourceId", new { ResourceId = resourceId }) == 1;
    }
}
