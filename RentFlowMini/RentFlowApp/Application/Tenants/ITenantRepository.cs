using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Tenants;

public interface ITenantRepository
{
    void Add(Tenant tenant);

    Tenant? GetById(TenantId tenantId);

    IReadOnlyList<Tenant> GetAll();
}
