using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public interface IInvoiceSettingsRepository
{
    InvoiceSettings Get();

    void Save(InvoiceSettings settings);
}
