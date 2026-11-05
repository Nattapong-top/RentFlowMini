using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Tenants;

public sealed class MoveOutTenantService
{
    private readonly ITenantRepository _repository;

    public MoveOutTenantService(ITenantRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Tenant MoveOut(TenantId tenantId, DateOnly movedOutOn)
    {
        if (movedOutOn > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new InvalidOperationException("วันที่ย้ายออกต้องไม่เกินวันนี้");
        }

        var tenant = _repository.GetById(tenantId)
            ?? throw new InvalidOperationException("ไม่พบผู้เช่าที่เลือก กรุณาเลือกจากตารางอีกครั้ง");

        var movedOut = tenant.MoveOut(movedOutOn);
        _repository.Update(movedOut);

        return movedOut;
    }
}
