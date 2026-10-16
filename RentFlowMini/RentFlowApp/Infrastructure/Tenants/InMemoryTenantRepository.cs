using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Tenants;

public sealed class InMemoryTenantRepository : ITenantRepository
{
    private readonly List<Tenant> _tenants = [];

    public void Add(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        if (GetById(tenant.Id) is not null)
        {
            throw new InvalidOperationException(
                $"Tenant '{tenant.Id.Value}' already exists.");
        }

        var roomId = tenant.RoomId;
        if (roomId is not null && ExistsByRoomId(roomId))
        {
            throw new InvalidOperationException(
                "ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้");
        }

        _tenants.Add(tenant);
    }

    public Tenant? GetById(TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        return _tenants.Find(tenant => tenant.Id == tenantId);
    }

    public bool ExistsByRoomId(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        return _tenants.Exists(tenant => tenant.RoomId == roomId);
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        return _tenants.AsReadOnly();
    }
}
