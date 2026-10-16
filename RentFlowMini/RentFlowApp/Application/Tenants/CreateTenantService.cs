using RentFlowApp.Domain;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Tenants;

public sealed class CreateTenantService
{
    private readonly ITenantRepository _repository;

    public CreateTenantService(ITenantRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Tenant Create(
        string firstName,
        string lastName,
        string? phoneNumber = null,
        RoomId? roomId = null)
    {
        if (roomId is not null && _repository.ExistsByRoomId(roomId))
        {
            throw new InvalidOperationException(
                "ห้องพักนี้มีผู้เช่าอยู่แล้ว ไม่สามารถผูกผู้เช่าซ้ำได้");
        }

        var tenant = new Tenant(
            TenantId.New(),
            firstName,
            lastName,
            phoneNumber,
            roomId);

        _repository.Add(tenant);

        return tenant;
    }
}
