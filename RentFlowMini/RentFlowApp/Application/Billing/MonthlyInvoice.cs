namespace RentFlowApp.Application.Billing;

public sealed record MonthlyInvoiceLine(
    string Description,
    string MeterRange,
    string Units,
    string UnitPrice,
    string Amount);

public sealed record MonthlyInvoice(
    string PropertyName,
    string Address,
    string PaymentChannels,
    string BankAccountDetails,
    string InvoiceNumber,
    string RoomNumber,
    string BillingMonth,
    string IssueDate,
    string DueDate,
    string TenantName,
    IReadOnlyList<MonthlyInvoiceLine> Lines,
    string GrandTotal,
    string PaymentStatus);
