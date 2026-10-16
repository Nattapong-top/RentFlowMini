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

        Assert.Same(tenant, repository.GetById(tenant.Id));
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
    public void Add_WithSameRoomForDifferentTenants_IsAllowed()
    {
        var repository = new InMemoryTenantRepository();
        var roomId = RoomId.New();

        repository.Add(new Tenant(TenantId.New(), "สมชาย", "ใจดี", roomId: roomId));
        repository.Add(new Tenant(TenantId.New(), "สมหญิง", "ใจดี", roomId: roomId));

        Assert.Equal(2, repository.GetAll().Count);
    }
}
