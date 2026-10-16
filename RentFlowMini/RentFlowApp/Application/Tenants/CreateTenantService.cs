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
