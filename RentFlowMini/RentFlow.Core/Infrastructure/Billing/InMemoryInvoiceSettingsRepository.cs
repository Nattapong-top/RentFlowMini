using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class InMemoryInvoiceSettingsRepository : IInvoiceSettingsRepository
{
    private InvoiceSettings _settings = InvoiceSettings.Empty;

    public InvoiceSettings Get() => _settings;

    public void Save(InvoiceSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }
}
