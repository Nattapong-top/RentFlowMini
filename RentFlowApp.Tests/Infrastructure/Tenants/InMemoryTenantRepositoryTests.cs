using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.Infrastructure.Tenants;

public class InMemoryTenantRepositoryTests
{
    [Fact]
    public void GetAll_WithNoTenants_ReturnsEmptyList()
    {
        var repository = new InMemoryTenantRepository();

        Assert.Empty(repository.GetAll());
    }

    [Fact]
    public void Add_WithValidTenant_CanBeReadById()
    {
        var repository = new InMemoryTenantRepository();
        var tenant = new Tenant(TenantId.New(), "สมชาย", "ใจดี");

        repository.Add(tenant);

        var savedTenant = repository.GetById(tenant.Id);

        Assert.NotNull(savedTenant);
        Assert.Equal(tenant.Id, savedTenant.Id);
        Assert.Equal(tenant.FirstName, savedTenant.FirstName);
        Assert.Equal(tenant.LastName, savedTenant.LastName);
        Assert.NotSame(tenant, savedTenant);
    }

    [Fact]
    public void Add_WithDuplicateId_ThrowsException()
    {
        var repository = new InMemoryTenantRepository();
        var tenantId = TenantId.New();

        repository.Add(new Tenant(tenantId, "สมชาย", "ใจดี"));

        Assert.Throws<InvalidOperationException>(() => repository.Add(
            new Tenant(tenantId, "สมหญิง", "ใจดี")));
    }

    [Fact]
    public void Add_WithOccupiedRoom_ThrowsException()
    {
        var repository = new InMemoryTenantRepository();
        var roomId = RoomId.New();

        repository.Add(new Tenant(TenantId.New(), "สมชาย", "ใจดี", roomId: roomId));

        Assert.Throws<InvalidOperationException>(() => repository.Add(
            new Tenant(TenantId.New(), "สมหญิง", "ใจดี", roomId: roomId)));
    }

    [Fact]
    public void ExistsByRoomId_WhenRoomHasTenant_ReturnsTrue()
    {
        var repository = new InMemoryTenantRepository();
        var roomId = RoomId.New();

        repository.Add(new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            roomId: roomId));

        Assert.True(repository.ExistsByRoomId(roomId));
    }

    [Fact]
    public void Add_WithPhoneAndRoom_PreservesAllValuesWhenReadById()
    {
        var repository = new InMemoryTenantRepository();
        var tenant = new Tenant(
            TenantId.New(),
            "สมชาย",
            "ใจดี",
            "0812345678",
            RoomId.New());

        repository.Add(tenant);

        var savedTenant = repository.GetById(tenant.Id);

        Assert.NotNull(savedTenant);
        Assert.Equal(tenant.PhoneNumber, savedTenant.PhoneNumber);
        Assert.Equal(tenant.RoomId, savedTenant.RoomId);
    }

    [Fact]
    public void GetAll_ReturnsTenantsOrderedByFirstNameAndLastName()
    {
        var repository = new InMemoryTenantRepository();
        var second = new Tenant(TenantId.New(), "สมหญิง", "ใจดี");
        var first = new Tenant(TenantId.New(), "สมชาย", "ใจดี");

        repository.Add(second);
        repository.Add(first);

        var tenants = repository.GetAll();

        Assert.Equal(first.Id, tenants[0].Id);
        Assert.Equal(second.Id, tenants[1].Id);
    }
}
