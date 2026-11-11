using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class UpdateBillingSettingsService
{
    private readonly IBillingSettingsRepository _repository;

    public UpdateBillingSettingsService(IBillingSettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public BillingSettings Update(
        Money waterRate,
        Money electricityRate,
        Money motorcycleParkingRate,
        Money carParkingRate,
        Money cableTvRate)
    {
        ArgumentNullException.ThrowIfNull(waterRate);
        ArgumentNullException.ThrowIfNull(electricityRate);
        ArgumentNullException.ThrowIfNull(motorcycleParkingRate);
        ArgumentNullException.ThrowIfNull(carParkingRate);
        ArgumentNullException.ThrowIfNull(cableTvRate);

        var settings = BillingSettings.Create(
            waterRate.Amount,
            electricityRate.Amount,
            motorcycleParkingRate.Amount,
            carParkingRate.Amount,
            cableTvRate.Amount);

        _repository.Save(settings);
        return settings;
    }
}
