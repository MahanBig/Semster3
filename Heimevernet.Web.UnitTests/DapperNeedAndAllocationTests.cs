using Dapper;
using Heimevernet.Web.DataAccess;
using Heimevernet.Web.Models;
using Heimevernet.Web.Models.Entities;

namespace Heimevernet.Web.UnitTests;

public class DapperNeedAndAllocationTests
{
    private const int Water = 2; // Category id in SqliteTestDatabase.

    private static (DapperNeedRepository Needs, DapperResourceRepository Resources, DapperAllocationRepository Allocations) Repositories(
        SqliteTestDatabase database) =>
        (new DapperNeedRepository(database), new DapperResourceRepository(database), new DapperAllocationRepository(database));

    private static Need WaterNeed(int quantity = 5) => new()
    {
        Title = "Vann til sykehjem", Description = "Trenger drikkevann", Quantity = quantity,
        Priority = NeedPriority.Critical, Status = NeedStatus.Open, RegisteredAt = DateTime.UtcNow,
        CategoryId = Water, UserId = "kari-id"
    };

    private static Resource WaterResource(string name, int quantity) => new()
    {
        Name = name, Description = "Vann", Quantity = quantity, CategoryId = Water,
        Status = ResourceStatus.Available, UserId = "ola-id"
    };

    [Fact]
    public void AddAndGetById_SavesNeedWithCategoryAndAddress()
    {
        using var database = new SqliteTestDatabase();
        var (needs, _, _) = Repositories(database);
        var need = WaterNeed();
        need.Address = "Kirkegata 2";
        need.PostalCode = "4630";
        need.PostalPlace = "Kristiansand S";
        need.Deadline = new DateTime(2026, 12, 1);

        var id = needs.Add(need);
        var saved = needs.GetById(id);

        Assert.NotNull(saved);
        Assert.Equal("Vann til sykehjem", saved.Title);
        Assert.Equal("Vann", saved.CategoryName);
        Assert.Equal(NeedPriority.Critical, saved.Priority);
        Assert.Equal(NeedStatus.Open, saved.Status);
        Assert.Equal("Kirkegata 2", saved.Address);
        Assert.Equal("Kristiansand S", saved.PostalPlace);
        Assert.Equal(new DateTime(2026, 12, 1), saved.Deadline);
        Assert.Equal(0, saved.AllocatedQuantity);
        Assert.Null(needs.GetById(999));
    }

    [Fact]
    public void GetAll_PutsCriticalFirstAndCoveredLast()
    {
        using var database = new SqliteTestDatabase();
        var (needs, _, _) = Repositories(database);
        var low = WaterNeed(); low.Title = "Lav"; low.Priority = NeedPriority.Low;
        var critical = WaterNeed(); critical.Title = "Kritisk"; critical.Priority = NeedPriority.Critical;
        var coveredCritical = WaterNeed(); coveredCritical.Title = "Dekket"; coveredCritical.Status = NeedStatus.Covered;
        needs.Add(low);
        needs.Add(coveredCritical);
        needs.Add(critical);

        var titles = needs.GetAll().Select(need => need.Title).ToList();

        Assert.Equal(["Kritisk", "Lav", "Dekket"], titles);
    }

    [Fact]
    public void Allocate_UpdatesStatusOnNeedAndResource()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed(quantity: 5));
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        var cansId = resources.Add(WaterResource("Kanner", 10));

        Assert.True(allocations.Allocate(needId, tankId, 3));

        Assert.Equal(NeedStatus.InProgress, needs.GetById(needId)!.Status);
        Assert.Equal(3, needs.GetById(needId)!.AllocatedQuantity);
        Assert.Equal(ResourceStatus.InUse, resources.GetById(tankId)!.Status);
        Assert.Equal(ResourceStatus.Available, resources.GetById(cansId)!.Status);

        Assert.True(allocations.Allocate(needId, cansId, 2));

        Assert.Equal(NeedStatus.Covered, needs.GetById(needId)!.Status);
        Assert.Equal(5, needs.GetById(needId)!.AllocatedQuantity);
        var rows = allocations.GetForNeed(needId);
        Assert.Equal(["Vanntank", "Kanner"], rows.Select(row => row.ResourceName).ToList());
    }

    [Fact]
    public void Allocate_SamePairTwice_ReturnsFalseAndChangesNothing()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed());
        var tankId = resources.Add(WaterResource("Vanntank", 3));

        Assert.True(allocations.Allocate(needId, tankId, 1));
        Assert.False(allocations.Allocate(needId, tankId, 2));

        Assert.Equal(1, Assert.Single(allocations.GetForNeed(needId)).Quantity);
    }

    [Fact]
    public void Remove_SetsStatusesBack()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed());
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        allocations.Allocate(needId, tankId, 3);

        Assert.True(allocations.Remove(needId, tankId));
        Assert.False(allocations.Remove(needId, tankId));

        Assert.Equal(NeedStatus.Open, needs.GetById(needId)!.Status);
        Assert.Equal(ResourceStatus.Available, resources.GetById(tankId)!.Status);
        Assert.Empty(allocations.GetForNeed(needId));
    }

    [Fact]
    public void Update_LoweringQuantity_RecalculatesStatus()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed(quantity: 5));
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        allocations.Allocate(needId, tankId, 3);

        var changed = WaterNeed(quantity: 3);
        var updated = needs.Update(new Need
        {
            Id = needId, Title = changed.Title, Description = changed.Description, Quantity = 3,
            Priority = changed.Priority, CategoryId = Water
        });

        Assert.True(updated);
        Assert.Equal(NeedStatus.Covered, needs.GetById(needId)!.Status);
        Assert.Equal("kari-id", needs.GetById(needId)!.UserId);
    }

    [Fact]
    public void Delete_RemovesAllocationsAndFreesTheResource()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed());
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        allocations.Allocate(needId, tankId, 3);

        Assert.True(needs.Delete(needId));

        Assert.Null(needs.GetById(needId));
        Assert.Equal(ResourceStatus.Available, resources.GetById(tankId)!.Status);
        using var connection = database.CreateConnection();
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Allocations"));
    }

    [Fact]
    public void DeletingAResource_RemovesItsAllocations()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed());
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        allocations.Allocate(needId, tankId, 3);

        resources.Delete(tankId);

        Assert.Empty(allocations.GetForNeed(needId));
    }

    [Fact]
    public void UnavailableResource_KeepsItsStatusWhenAllocationIsRemoved()
    {
        using var database = new SqliteTestDatabase();
        var (needs, resources, allocations) = Repositories(database);
        var needId = needs.Add(WaterNeed());
        var tankId = resources.Add(WaterResource("Vanntank", 3));
        allocations.Allocate(needId, tankId, 1);
        using (var connection = database.CreateConnection())
        {
            connection.Execute("UPDATE Resources SET Status = 'utilgjengelig' WHERE Id = @Id", new { Id = tankId });
        }

        allocations.Remove(needId, tankId);

        Assert.Equal(ResourceStatus.Unavailable, resources.GetById(tankId)!.Status);
    }
}
