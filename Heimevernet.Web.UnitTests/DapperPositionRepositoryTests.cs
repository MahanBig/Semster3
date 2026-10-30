using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class DapperPositionRepositoryTests
{
    [Fact]
    public void AddAndGetAll_PersistPositionsInOrder()
    {
        using var database = new SqliteTestDatabase();
        var repository = new DapperPositionRepository(database);
        var createdAt = new DateTime(2026, 10, 30, 21, 15, 0, DateTimeKind.Utc);

        var firstId = repository.Add(new Position
        {
            Latitude = 58.2475, Longitude = 8.3778, Description = "Brannstasjon", CreatedBy = "ola@test.no", CreatedAtUtc = createdAt
        });
        var secondId = repository.Add(new Position
        {
            Latitude = -33.9, Longitude = 151.2, Description = "Sydney", CreatedBy = null, CreatedAtUtc = createdAt
        });

        var positions = repository.GetAll();

        Assert.True(firstId > 0);
        Assert.True(secondId > firstId);
        Assert.Collection(positions,
            position =>
            {
                Assert.Equal(firstId, position.Id);
                Assert.Equal(58.2475, position.Latitude);
                Assert.Equal(8.3778, position.Longitude);
                Assert.Equal("Brannstasjon", position.Description);
                Assert.Equal("ola@test.no", position.CreatedBy);
                Assert.Equal(createdAt, position.CreatedAtUtc);
            },
            position =>
            {
                Assert.Equal(secondId, position.Id);
                Assert.Null(position.CreatedBy);
            });
    }
}
