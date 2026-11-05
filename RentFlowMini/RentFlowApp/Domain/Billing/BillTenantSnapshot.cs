using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed record BillTenantSnapshot(TenantId TenantId, string FullName)
{
    public static BillTenantSnapshot From(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new BillTenantSnapshot(tenant.Id, tenant.FullName);
    }
}
