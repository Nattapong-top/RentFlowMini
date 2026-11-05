using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class MonthlyInvoiceBuilder
{
    private const string NoValue = "-";

    private readonly IRoomRepository _roomRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IInvoiceSettingsRepository _settingsRepository;

    public MonthlyInvoiceBuilder(
        IRoomRepository roomRepository,
        ITenantRepository tenantRepository,
        IInvoiceSettingsRepository settingsRepository)
    {
        ArgumentNullException.ThrowIfNull(roomRepository);
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(settingsRepository);

        _roomRepository = roomRepository;
        _tenantRepository = tenantRepository;
        _settingsRepository = settingsRepository;
    }

    public IReadOnlyList<MonthlyInvoice> Build(IEnumerable<Bill> bills)
    {
        ArgumentNullException.ThrowIfNull(bills);

        var settings = _settingsRepository.Get();
        var roomNumbers = _roomRepository.GetAll().ToDictionary(room => room.Id, room => room.RoomNumber.Value);
        var tenants = _tenantRepository.GetAll()
            .Where(tenant => tenant.RoomId != null)
            .GroupBy(tenant => tenant.RoomId!)
            .ToDictionary(group => group.Key, group => group.First());

        return bills
            .Select(bill => new MonthlyInvoice(
                settings.PropertyName,
                settings.Address,
                "ชำระด้วยเงินสด หรือโอนเข้าบัญชี",
                settings.BankAccountDetails,
                FormatInvoiceNumber(bill.InvoiceNumber),
                roomNumbers.GetValueOrDefault(bill.RoomId, NoValue),
                $"{bill.BillingPeriod.Month:D2}/{bill.BillingPeriod.Year + 543}",
                FormatThaiDate(DateOnly.FromDateTime(bill.CreatedAt)),
                FormatThaiDate(bill.DueDate),
                tenants.TryGetValue(bill.RoomId, out var tenant) ? $"คุณ {tenant.FirstName} {tenant.LastName}" : NoValue,
                BuildLines(bill),
                FormatAmount(bill.Total)))
            .ToList();
    }

    private static IReadOnlyList<MonthlyInvoiceLine> BuildLines(Bill bill)
    {
        var meters = bill.Meters;

        return
        [
            FixedChargeLine("ค่าเช่าห้อง (Room Rent)", bill.Rent),
            new MonthlyInvoiceLine(
                "ค่าน้ำประปา (Water Supply)",
                meters is null ? string.Empty : FormatMeterRange(meters.PreviousWater, meters.CurrentWater),
                meters is null ? string.Empty : meters.WaterUsage.ToString("N2"),
                meters is null ? string.Empty : meters.WaterRate.ToString("N2"),
                FormatAmount(bill.Water)),
            new MonthlyInvoiceLine(
                "ค่าไฟฟ้า (Electric Supply)",
                meters is null ? string.Empty : FormatMeterRange(meters.PreviousElectricity, meters.CurrentElectricity),
                meters is null ? string.Empty : meters.ElectricityUsage.ToString("N2"),
                meters is null ? string.Empty : meters.ElectricityRate.ToString("N2"),
                FormatAmount(bill.Electricity)),
            FixedChargeLine("ค่าจอดมอเตอร์ไซค์ (Motorcycle Parking)", bill.MotorcycleParking),
            FixedChargeLine("ค่าจอดรถยนต์ (Car Parking)", bill.CarParking),
            FixedChargeLine("ค่าเคเบิลทีวี (Cable TV)", bill.CableTv)
        ];
    }

    private static MonthlyInvoiceLine FixedChargeLine(string description, Money amount) =>
        new(description, string.Empty, string.Empty, string.Empty, FormatAmount(amount));

    private static string FormatMeterRange(MeterReading previous, MeterReading current) =>
        $"( {previous.Value:0.##} - {current.Value:0.##} )";

    private static string FormatInvoiceNumber(string invoiceNumber) =>
        invoiceNumber.StartsWith('N') ? $"N.{invoiceNumber[1..]}" : invoiceNumber;

    private static string FormatThaiDate(DateOnly date) =>
        $"{date.Day:D2}/{date.Month:D2}/{date.Year + 543}";

    private static string FormatAmount(Money money)
    {
        if (money.Amount == 0m)
        {
            return NoValue;
        }

        return money.Amount % 1 == 0 ? $"{money.Amount:N0}.-" : money.Amount.ToString("N2");
    }
}
