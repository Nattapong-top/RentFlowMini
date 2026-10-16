using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Tenants;

public interface ITenantRepository
{
    void Add(Tenant tenant);

    Tenant? GetById(TenantId tenantId);

    bool ExistsByRoomId(RoomId roomId);

    IReadOnlyList<Tenant> GetAll();
}
