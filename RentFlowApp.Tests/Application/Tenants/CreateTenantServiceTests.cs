using RentFlowApp.Domain;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Tenants;

public class CreateTenantServiceTests
{
    [Fact]
    public void Create_WithValidData_ReturnsTenant()
    {
        var service = new CreateTenantService(new FakeTenantRepository());

        var tenant = service.Create("สมชาย", "ใจดี", "0812345678");

        Assert.Equal("สมชาย", tenant.FirstName);
        Assert.Equal("ใจดี", tenant.LastName);
        Assert.Equal("0812345678", tenant.PhoneNumber);
    }

    [Fact]
    public void Create_WithValidData_SavesTenant()
    {
        var repository = new FakeTenantRepository();
        var service = new CreateTenantService(repository);

        var tenant = service.Create("สมชาย", "ใจดี");

        Assert.Contains(tenant, repository.Tenants);
    }

    [Fact]
    public void Create_WithRoom_AssignsOneRoomToTenant()
    {
        var service = new CreateTenantService(new FakeTenantRepository());
        var roomId = RoomId.New();

        var tenant = service.Create("สมชาย", "ใจดี", roomId: roomId);

        Assert.Same(roomId, tenant.RoomId);
    }

    private sealed class FakeTenantRepository : ITenantRepository
    {
        public List<Tenant> Tenants { get; } = [];

        public void Add(Tenant tenant)
        {
            Tenants.Add(tenant);
        }

        public Tenant? GetById(TenantId tenantId)
        {
            return Tenants.Find(tenant => tenant.Id == tenantId);
        }

        public IReadOnlyList<Tenant> GetAll()
        {
            return Tenants.AsReadOnly();
        }
    }
}
