using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class InMemoryBillingSettingsRepository : IBillingSettingsRepository
{
    private BillingSettings _settings = BillingSettings.Create(0m, 0m, 0m, 0m, 0m);

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
