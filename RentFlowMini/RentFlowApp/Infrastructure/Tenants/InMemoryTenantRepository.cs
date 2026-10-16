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

        _tenants.Add(tenant);
    }

    public Tenant? GetById(TenantId tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        return _tenants.Find(tenant => tenant.Id == tenantId);
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        return _tenants.AsReadOnly();
    }
}
