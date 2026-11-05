using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Application.Tenants;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;
using RentFlowApp.Infrastructure.Tenants;

namespace RentFlowApp.Tests.Application.Billing;

public class MonthlyInvoiceBuilderTests
{
    private readonly InMemoryRoomRepository _rooms = new();
    private readonly InMemoryTenantRepository _tenants = new();
    private readonly InMemoryInvoiceSettingsRepository _settings = new();
    private readonly InMemoryBillRepository _bills = new();

    private MonthlyInvoiceBuilder CreateBuilder() => new(_rooms, _tenants, _settings);

    private Bill CreateBill(RoomId roomId, decimal rent = 3000m, decimal waterRate = 19m)
    {
        return new CreateMonthlyBillService(_bills).Create(new CreateMonthlyBillRequest(
            roomId,
            BillingPeriod.Create("2026-10"),
            rent,
            MeterReading.Create(773m),
            MeterReading.Create(780m),
            MeterReading.Create(1713m),
            MeterReading.Create(1822m),
            waterRate,
            8m,
            IsCableTvExempt: false,
            CableTvRate: 60m));
    }

    [Fact]
    public void Build_MapsBillToInvoiceLikeCurrentPaperForm()
    {
        var room = new CreateRoomService(_rooms).Create("303", 3000m, false);
        new CreateTenantService(_tenants).Create("สมชาย", "ใจดี", "0811111111", room.Id);
        var bill = CreateBill(room.Id);

        var invoice = Assert.Single(CreateBuilder().Build([bill]));

        Assert.Equal("N.001", invoice.InvoiceNumber);
        Assert.Equal("303", invoice.RoomNumber);
        Assert.Equal("10/2569", invoice.BillingMonth);
        Assert.Equal("05/11/2569", invoice.DueDate);
        Assert.Equal("คุณ สมชาย ใจดี", invoice.TenantName);
        Assert.Equal("4,065.-", invoice.GrandTotal);

        Assert.Equal(6, invoice.Lines.Count);
        Assert.Contains("ค่าเช่าห้อง", invoice.Lines[0].Description);
        Assert.Equal("3,000.-", invoice.Lines[0].Amount);
        var water = invoice.Lines[1];
        Assert.Equal("( 773 - 780 )", water.MeterRange);
        Assert.Equal("7.00", water.Units);
        Assert.Equal("19.00", water.UnitPrice);
        Assert.Equal("133.-", water.Amount);
        var electricity = invoice.Lines[2];
        Assert.Equal("( 1713 - 1822 )", electricity.MeterRange);
        Assert.Equal("109.00", electricity.Units);
        Assert.Equal("872.-", electricity.Amount);
    }

    [Fact]
    public void Build_ListsParkingAndCableTvOnSeparateLines()
    {
        var room = new CreateRoomService(_rooms).Create("303", 3000m, false);
        var bill = CreateBill(room.Id);
        bill.UpdateAdditionalCharges(Money.Create(100m), Money.Create(0m), Money.Create(60m));

        var lines = CreateBuilder().Build([bill])[0].Lines;

        Assert.Contains("มอเตอร์ไซค์", lines[3].Description);
        Assert.Equal("100.-", lines[3].Amount);
        Assert.Contains("รถยนต์", lines[4].Description);
        Assert.Equal("-", lines[4].Amount);
        Assert.Contains("เคเบิล", lines[5].Description);
        Assert.Equal("60.-", lines[5].Amount);
    }

    [Fact]
    public void Build_ShowsSatangWhenAmountIsNotWholeNumber()
    {
        var room = new CreateRoomService(_rooms).Create("303", 3000m, false);
        var bill = CreateBill(room.Id, waterRate: 19.5m);

        var invoice = CreateBuilder().Build([bill])[0];

        Assert.Equal("136.50", invoice.Lines[1].Amount);
    }

    [Fact]
    public void Build_WhenRoomHasNoTenant_ShowsPlaceholderName()
    {
        var room = new CreateRoomService(_rooms).Create("303", 3000m, false);
        var bill = CreateBill(room.Id);

        Assert.Equal("-", CreateBuilder().Build([bill])[0].TenantName);
    }

    [Fact]
    public void Build_UsesPropertyDetailsAndPaymentChannelsFromSettings()
    {
        var room = new CreateRoomService(_rooms).Create("303", 3000m, false);
        var bill = CreateBill(room.Id);
        _settings.Save(InvoiceSettings.Create("หอทดสอบ", "ที่อยู่ทดสอบ", "ธนาคารทดสอบ 000-0-00000-0"));

        var invoice = CreateBuilder().Build([bill])[0];

        Assert.Equal("หอทดสอบ", invoice.PropertyName);
        Assert.Equal("ที่อยู่ทดสอบ", invoice.Address);
        Assert.Contains("เงินสด", invoice.PaymentChannels);
        Assert.Contains("โอนเข้าบัญชี", invoice.PaymentChannels);
        Assert.Equal("ธนาคารทดสอบ 000-0-00000-0", invoice.BankAccountDetails);
    }
}
