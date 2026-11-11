namespace RentFlowApp.Domain.Billing;

public sealed record InvoiceSettings
{
    private InvoiceSettings(string propertyName, string address, string bankAccountDetails)
    {
        PropertyName = propertyName;
        Address = address;
        BankAccountDetails = bankAccountDetails;
    }

    public string PropertyName { get; }

    public string Address { get; }

    public string BankAccountDetails { get; }

    public static InvoiceSettings Empty { get; } = new(string.Empty, string.Empty, string.Empty);

    public bool IsConfigured => PropertyName.Length > 0 && Address.Length > 0;

    public static InvoiceSettings Create(string? propertyName, string? address, string? bankAccountDetails)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("กรุณาระบุชื่อหอพัก", nameof(propertyName));
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("กรุณาระบุที่อยู่หอพัก", nameof(address));
        }

        return new InvoiceSettings(
            propertyName.Trim(),
            address.Trim(),
            bankAccountDetails?.Trim() ?? string.Empty);
    }
}
