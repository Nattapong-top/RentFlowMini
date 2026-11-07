using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests;

// A bill can be issued only for a room with a tenant, so tests that are not about tenants use these.
internal static class TestTenants
{
    public static BillTenantSnapshot Snapshot() => new(TenantId.New(), "ผู้เช่า ทดสอบ");

    public static Tenant MoveInto(ITenantRepository tenants, RoomId roomId) =>
        new CreateTenantService(tenants).Create("ผู้เช่า", "ทดสอบ", null, roomId);
}
