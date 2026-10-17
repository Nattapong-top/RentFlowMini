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
                "ผู้เช่ารายนี้มีอยู่แล้ว ไม่สามารถเพิ่มข้อมูลซ้ำได้");
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

        var tenant = _tenants.Find(item => item.Id == tenantId);
        return tenant is null ? null : CopyTenant(tenant);
    }

    public bool ExistsByRoomId(RoomId roomId)
    {
        ArgumentNullException.ThrowIfNull(roomId);

        return _tenants.Exists(tenant => tenant.RoomId == roomId);
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        return _tenants
            .OrderBy(tenant => tenant.FirstName)
            .ThenBy(tenant => tenant.LastName)
            .Select(CopyTenant)
            .ToList()
            .AsReadOnly();
    }

    private static Tenant CopyTenant(Tenant tenant)
    {
        return new Tenant(
            TenantId.From(tenant.Id.Value),
            tenant.FirstName,
            tenant.LastName,
            tenant.PhoneNumber,
            tenant.RoomId is null ? null : RoomId.From(tenant.RoomId.Value));
    }
}
