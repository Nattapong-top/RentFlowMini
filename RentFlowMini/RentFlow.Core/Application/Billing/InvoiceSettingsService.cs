using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public sealed class InvoiceSettingsService
{
    private readonly IInvoiceSettingsRepository _repository;

    public InvoiceSettingsService(IInvoiceSettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public InvoiceSettings GetCurrent() => _repository.Get();

    public InvoiceSettings Save(string? propertyName, string? address, string? bankAccountDetails)
    {
        try
        {
            var settings = InvoiceSettings.Create(propertyName, address, bankAccountDetails);
            _repository.Save(settings);
            return settings;
        }
        catch (ArgumentException ex) when (ex.ParamName is not null)
        {
            // .NET เติม "(Parameter '...')" ท้ายข้อความเมื่อมี ParamName จึงโยนใหม่โดยไม่ใส่ ข้อความจะแสดงให้ผู้ใช้ได้ทันที
            throw new ArgumentException(ex.Message.Split(" (Parameter")[0], ex);
        }
    }
}
