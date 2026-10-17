using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class InMemoryBillingSettingsRepository : IBillingSettingsRepository
{
    private BillingSettings _settings = BillingSettings.Create(
        waterRate: 19.00m,
        electricityRate: 8.00m,
        motorcycleParkingRate: 100.00m,
        carParkingRate: 500.00m,
        cableTvRate: 60.00m);

    public BillingSettings Get()
    {
        return _settings;
    }

    public void Save(BillingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }
}
