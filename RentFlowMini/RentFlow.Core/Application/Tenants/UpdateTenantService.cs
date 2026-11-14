using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Tenants;

public sealed record UpdateTenantRequest(
    TenantId TenantId,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    RoomId? RoomId,
    DateOnly? MoveInDate,
    decimal? SecurityDeposit);

public sealed class UpdateTenantService
{
    private readonly ITenantRepository _repository;

    public UpdateTenantService(ITenantRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Tenant Update(UpdateTenantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tenant = _repository.GetById(request.TenantId)
            ?? throw new InvalidOperationException("ไม่พบผู้เช่าที่เลือก กรุณาเลือกจากตารางอีกครั้ง");

        if (request.RoomId is not null && request.RoomId != tenant.RoomId && _repository.ExistsByRoomId(request.RoomId))
        {
            throw new InvalidOperationException("ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้");
        }

        var updated = tenant.WithDetails(
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.RoomId,
            request.MoveInDate,
            request.SecurityDeposit);

        _repository.Update(updated);

        return updated;
    }
}
