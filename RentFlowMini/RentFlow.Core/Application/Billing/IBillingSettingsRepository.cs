using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public interface IBillingSettingsRepository
{
    BillingSettings Get();

    void Save(BillingSettings settings);
}
