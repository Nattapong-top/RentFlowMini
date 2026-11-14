using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class SaveMonthlyMeterReadingServiceTests
{
    [Fact]
    public void Save_WhenValidReading_SavesToRepository()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

        Assert.True(repository.Exists(roomId, period));
        var saved = repository.Get(roomId, period);
        Assert.NotNull(saved);
        Assert.Equal(100m, saved.PreviousWaterMeter.Value);
        Assert.Equal(110m, saved.CurrentWaterMeter.Value);
        Assert.Equal(500m, saved.PreviousElectricityMeter.Value);
        Assert.Equal(520m, saved.CurrentElectricityMeter.Value);
    }

    [Fact]
    public void Save_WhenReadingAlreadyExistsForPeriod_UpdatesSuccessfully()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

        // Save again with updated current readings
        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(500m),
            MeterReading.Create(550m));

        var saved = repository.Get(roomId, period);
        Assert.NotNull(saved);
        Assert.Equal(125m, saved.CurrentWaterMeter.Value);
        Assert.Equal(550m, saved.CurrentElectricityMeter.Value);
    }

    [Fact]
    public void Save_WhenCurrentMeterLessThanPrevious_ThrowsInvalidMeterReadingException()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        Assert.Throws<InvalidMeterReadingException>(() =>
            service.Save(
                roomId,
                period,
                MeterReading.Create(110m),
                MeterReading.Create(100m),
                MeterReading.Create(500m),
                MeterReading.Create(520m)));
    }

    private static readonly BillingPeriod October = BillingPeriod.Create("2026-10");

    private static Bill AddBill(InMemoryBillRepository bills, RoomId roomId, BillingPeriod period, bool isPaid)
    {
        var bill = Bill.Create(
            roomId,
            period,
            new BillingCalculationResult(
                Money.Create(2800m),
                Money.Create(0m),
                Money.Create(0m),
                Money.Create(0m),
                Money.Create(0m),
                Money.Create(0m),
                Money.Create(2800m)));
        if (isPaid)
        {
            bill.RecordPayment(new DateOnly(2026, 11, 3), PaymentMethod.Cash);
        }

        bills.Add(bill);
        return bill;
    }

    private static MeterReadingSaveResult SaveCurrent(
        SaveMonthlyMeterReadingService service,
        RoomId roomId,
        BillingPeriod period,
        decimal currentWater) =>
        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(currentWater),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

    [Fact]
    public void Save_WhenRoomHasNoBillForPeriod_SavesWithoutWarning()
    {
        var readings = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(readings, new InMemoryBillRepository());
        var roomId = RoomId.New();

        var result = SaveCurrent(service, roomId, October, 110m);

        Assert.Null(result.Warning);
        Assert.Equal(110m, readings.Get(roomId, October)!.CurrentWaterMeter.Value);
    }

    [Fact]
    public void Save_WhenUnpaidBillExistsForPeriod_SavesAndWarnsToRefreshBill()
    {
        var readings = new InMemoryMonthlyMeterReadingRepository();
        var bills = new InMemoryBillRepository();
        var roomId = RoomId.New();
        AddBill(bills, roomId, October, isPaid: false);
        var service = new SaveMonthlyMeterReadingService(readings, bills);

        var result = SaveCurrent(service, roomId, October, 115m);

        Assert.Equal(115m, readings.Get(roomId, October)!.CurrentWaterMeter.Value);
        Assert.NotNull(result.Warning);
        Assert.Contains("ดึงข้อมูลห้องใหม่", result.Warning);
    }

    [Fact]
    public void Save_WhenPaidBillExistsForPeriod_RejectsAndKeepsSavedReading()
    {
        var readings = new InMemoryMonthlyMeterReadingRepository();
        var bills = new InMemoryBillRepository();
        var roomId = RoomId.New();
        SaveCurrent(new SaveMonthlyMeterReadingService(readings), roomId, October, 110m);
        AddBill(bills, roomId, October, isPaid: true);
        var service = new SaveMonthlyMeterReadingService(readings, bills);

        var ex = Assert.Throws<InvalidOperationException>(() => SaveCurrent(service, roomId, October, 115m));

        Assert.Contains("ชำระแล้ว", ex.Message);
        Assert.Contains("ยกเลิกการรับชำระ", ex.Message);
        Assert.Equal(110m, readings.Get(roomId, October)!.CurrentWaterMeter.Value);
    }

    [Fact]
    public void Save_WhenPaidBillExistsForPeriodWithoutReading_RejectsWithoutSaving()
    {
        var readings = new InMemoryMonthlyMeterReadingRepository();
        var bills = new InMemoryBillRepository();
        var roomId = RoomId.New();
        AddBill(bills, roomId, October, isPaid: true);
        var service = new SaveMonthlyMeterReadingService(readings, bills);

        Assert.Throws<InvalidOperationException>(() => SaveCurrent(service, roomId, October, 115m));

        Assert.False(readings.Exists(roomId, October));
    }

    [Fact]
    public void Save_WhenPaidBillIsForAnotherPeriodOrRoom_SavesWithoutWarning()
    {
        var readings = new InMemoryMonthlyMeterReadingRepository();
        var bills = new InMemoryBillRepository();
        var roomId = RoomId.New();
        AddBill(bills, roomId, BillingPeriod.Create("2026-09"), isPaid: true);
        AddBill(bills, RoomId.New(), October, isPaid: true);
        var service = new SaveMonthlyMeterReadingService(readings, bills);

        var result = SaveCurrent(service, roomId, October, 110m);

        Assert.Null(result.Warning);
        Assert.True(readings.Exists(roomId, October));
    }
}
