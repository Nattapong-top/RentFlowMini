using RentFlowApp.Application.Billing;
using RentFlowApp.Application.Rooms;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;
using RentFlowApp.Infrastructure.Rooms;

namespace RentFlowApp.Tests.Application.Billing;

public class LatestRoomDataForBillServiceTests
{
    private static readonly BillingPeriod September = BillingPeriod.Create("2026-09");

    private readonly InMemoryBillRepository _bills = new();
    private readonly InMemoryRoomRepository _rooms = new();
    private readonly InMemoryMonthlyMeterReadingRepository _readings = new();

    private LatestRoomDataForBillService CreateService() => new(_bills, _rooms, _readings);

    // ห้องตอนนี้: ค่าเช่า 3500 และยกเว้น Cable TV ส่วนบิลออกไว้ด้วยค่าเช่า 3000 และไม่ยกเว้น
    private Bill CreateBillOfRoomThatChanged()
    {
        var room = new CreateRoomService(_rooms).Create("101", 3500m, true);
        var meter = MeterReading.Create(100m);

        return new CreateMonthlyBillService(_bills).Create(new CreateMonthlyBillRequest(
            room.Id, September, 3000m, meter, meter, meter, meter, 19m, 8m,
            IsCableTvExempt: false,
            CableTvRate: 60m,
            Tenant: TestTenants.Snapshot()));
    }

    [Fact]
    public void Get_ReturnsCurrentRoomRentExemptionAndMeterReadingsOfBillPeriod()
    {
        var bill = CreateBillOfRoomThatChanged();
        new SaveMonthlyMeterReadingService(_readings).Save(
            bill.RoomId,
            September,
            MeterReading.Create(100m),
            MeterReading.Create(120m),
            MeterReading.Create(500m),
            MeterReading.Create(510m));

        var latest = CreateService().Get(bill.Id);

        Assert.Equal(3500m, latest.MonthlyRent);
        Assert.True(latest.IsCableTvExempt);
        Assert.Equal(
            new BillMeterReadings(
                MeterReading.Create(100m),
                MeterReading.Create(120m),
                MeterReading.Create(500m),
                MeterReading.Create(510m)),
            latest.Meters);
        Assert.Null(latest.Notice);
    }

    [Fact]
    public void Get_WhenPeriodHasNoMeterReading_ReturnsNoMetersWithNotice()
    {
        var bill = CreateBillOfRoomThatChanged();

        var latest = CreateService().Get(bill.Id);

        Assert.Equal(3500m, latest.MonthlyRent);
        Assert.Null(latest.Meters);
        Assert.Equal("ไม่พบเลขมิเตอร์ของรอบบิลนี้ จึงใช้เลขมิเตอร์เดิมในบิล", latest.Notice);
    }

    [Fact]
    public void Get_WhenBillHasNoMeterSnapshot_ReturnsNoMetersWithNotice()
    {
        var room = new CreateRoomService(_rooms).Create("101", 3500m, true);
        var meter = MeterReading.Create(100m);
        // บิลที่บันทึกก่อนระบบเก็บเลขมิเตอร์และอัตราไว้ในบิล
        var bill = Bill.Create(room.Id, September, BillingCalculator.Calculate(3000m, meter, meter, meter, meter, 19m, 8m));
        _bills.Add(bill);
        new SaveMonthlyMeterReadingService(_readings).Save(
            room.Id,
            September,
            MeterReading.Create(100m),
            MeterReading.Create(120m),
            MeterReading.Create(500m),
            MeterReading.Create(510m));

        var latest = CreateService().Get(bill.Id);

        Assert.Equal(3500m, latest.MonthlyRent);
        Assert.True(latest.IsCableTvExempt);
        Assert.Null(latest.Meters);
        Assert.Equal("บิลนี้ไม่ได้เก็บอัตราค่าน้ำและค่าไฟไว้ จึงดึงได้เฉพาะค่าเช่าและสถานะยกเว้น Cable TV", latest.Notice);
    }

    [Fact]
    public void Get_WhenBillIsPaid_IsRejected()
    {
        var bill = CreateBillOfRoomThatChanged();
        new BillPaymentService(_bills).RecordPayment(bill.Id, new DateOnly(2026, 10, 3), PaymentMethod.Cash);

        var exception = Assert.Throws<InvalidOperationException>(() => CreateService().Get(bill.Id));

        Assert.Equal("บิลนี้ชำระแล้วจึงแก้ไขไม่ได้ กรุณายกเลิกการรับชำระก่อนแก้ไข", exception.Message);
    }

    [Fact]
    public void Get_WhenBillDoesNotExist_IsRejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => CreateService().Get(BillId.New()));

        Assert.Equal("ไม่พบบิลที่ต้องการแก้ไข กรุณาเลือกบิลจากตารางอีกครั้ง", exception.Message);
    }
}
