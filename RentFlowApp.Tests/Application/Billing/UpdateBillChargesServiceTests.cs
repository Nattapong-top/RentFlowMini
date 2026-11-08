using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class UpdateBillChargesServiceTests
{
    private readonly InMemoryBillRepository _repository = new();

    private Bill CreateStoredBill(
        bool chargeMotorcycle = false,
        bool chargeCar = false,
        bool isCableTvExempt = true)
    {
        var meterStart = MeterReading.Create(100m);
        var meterEnd = MeterReading.Create(110m);

        return new CreateMonthlyBillService(_repository).Create(new CreateMonthlyBillRequest(
            RoomId.New(),
            BillingPeriod.Create("2026-09"),
            3000m,
            meterStart,
            meterEnd,
            meterStart,
            meterEnd,
            19m,
            8m,
            chargeMotorcycle,
            100m,
            chargeCar,
            500m,
            isCableTvExempt,
            60m,
            TestTenants.Snapshot()));
    }

    [Fact]
    public void Update_WhenChargesAreAdded_RecalculatesChargesAndTotal()
    {
        var bill = CreateStoredBill();
        var service = new UpdateBillChargesService(_repository);

        var updated = service.Update(new UpdateBillChargesRequest(
            bill.Id, true, 100m, true, 500m, false, 60m));

        Assert.Equal(100m, updated.MotorcycleParking.Amount);
        Assert.Equal(500m, updated.CarParking.Amount);
        Assert.Equal(60m, updated.CableTv.Amount);
        Assert.Equal(3930m, updated.Total.Amount);
    }

    [Fact]
    public void Update_WhenChargesAreRemoved_ReducesTotal()
    {
        var bill = CreateStoredBill(chargeMotorcycle: true, chargeCar: true, isCableTvExempt: false);
        var service = new UpdateBillChargesService(_repository);

        var updated = service.Update(new UpdateBillChargesRequest(
            bill.Id, false, 100m, false, 500m, true, 60m));

        Assert.Equal(0m, updated.MotorcycleParking.Amount);
        Assert.Equal(0m, updated.CarParking.Amount);
        Assert.Equal(0m, updated.CableTv.Amount);
        Assert.Equal(3270m, updated.Total.Amount);
    }

    [Fact]
    public void Update_KeepsEverythingOutsideTheCheckboxCharges()
    {
        var bill = CreateStoredBill();
        var id = bill.Id;
        var invoiceNumber = bill.InvoiceNumber;
        var createdAt = bill.CreatedAt;
        var service = new UpdateBillChargesService(_repository);

        var updated = service.Update(new UpdateBillChargesRequest(
            id, true, 100m, false, 500m, true, 60m));

        Assert.Equal(id, updated.Id);
        Assert.Equal(invoiceNumber, updated.InvoiceNumber);
        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.Equal(3000m, updated.Rent.Amount);
        Assert.Equal(190m, updated.Water.Amount);
        Assert.Equal(80m, updated.Electricity.Amount);
        Assert.Single(_repository.GetAll());
    }

    [Fact]
    public void Update_WhenChargeStaysSelected_KeepsAmountOnBillEvenIfRateChanged()
    {
        var bill = CreateStoredBill(chargeMotorcycle: true);
        var service = new UpdateBillChargesService(_repository);

        var updated = service.Update(new UpdateBillChargesRequest(
            bill.Id, true, 999m, false, 500m, true, 60m));

        Assert.Equal(100m, updated.MotorcycleParking.Amount);
    }

    [Fact]
    public void Bill_RemembersCableTvExemption_SeparatelyFromZeroAmount()
    {
        var bill = CreateStoredBill(isCableTvExempt: true);
        var service = new UpdateBillChargesService(_repository);

        Assert.True(bill.IsCableTvExempt);

        // ราคากลางเป็น 0 ทำให้ยอดเป็น 0 เหมือนกัน แต่ห้องนี้ไม่ได้รับการยกเว้น
        var chargedAtZeroRate = service.Update(new UpdateBillChargesRequest(
            bill.Id, false, 100m, false, 500m, false, 0m));

        Assert.Equal(0m, chargedAtZeroRate.CableTv.Amount);
        Assert.False(chargedAtZeroRate.IsCableTvExempt);

        var exemptedAgain = service.Update(new UpdateBillChargesRequest(
            bill.Id, false, 100m, false, 500m, true, 60m));

        Assert.True(exemptedAgain.IsCableTvExempt);
    }

    // น้ำ 100 → 120 (20 หน่วย x 19 = 380), ไฟ 100 → 115 (15 หน่วย x 8 = 120)
    private static readonly BillMeterReadings CorrectedReadings = new(
        MeterReading.Create(100m),
        MeterReading.Create(120m),
        MeterReading.Create(100m),
        MeterReading.Create(115m));

    [Fact]
    public void Update_WithRentAndMeters_RecalculatesWithRatesOnBill()
    {
        var bill = CreateStoredBill();
        var service = new UpdateBillChargesService(_repository);

        var updated = service.Update(new UpdateBillChargesRequest(
            bill.Id, false, 100m, false, 500m, true, 60m,
            MonthlyRent: 3200m,
            Meters: CorrectedReadings));

        Assert.Equal(3200m, updated.Rent.Amount);
        Assert.Equal(380m, updated.Water.Amount);
        Assert.Equal(120m, updated.Electricity.Amount);
        Assert.Equal(3700m, updated.Total.Amount);
        Assert.Equal(3700m, _repository.Get(bill.Id)!.Total.Amount);
    }

    [Fact]
    public void Update_WithMetersOnBillWithoutMeterSnapshot_IsRejectedAndChangesNothing()
    {
        var meter = MeterReading.Create(100m);
        var bill = Bill.Create(RoomId.New(), BillingPeriod.Create("2026-09"), BillingCalculator.Calculate(3000m, meter, meter, meter, meter, 19m, 8m));
        _repository.Add(bill);
        var service = new UpdateBillChargesService(_repository);

        Assert.Throws<InvalidOperationException>(() => service.Update(new UpdateBillChargesRequest(
            bill.Id, true, 100m, false, 500m, true, 60m,
            MonthlyRent: 3200m,
            Meters: CorrectedReadings)));

        var stored = _repository.Get(bill.Id)!;
        Assert.Equal(3000m, stored.Rent.Amount);
        Assert.Equal(0m, stored.MotorcycleParking.Amount);
    }

    [Fact]
    public void Preview_ReturnsOldAndNewTotal_AndDoesNotChangeStoredBill()
    {
        var bill = CreateStoredBill();
        var service = new UpdateBillChargesService(_repository);

        var preview = service.Preview(new UpdateBillChargesRequest(
            bill.Id, true, 100m, false, 500m, true, 60m,
            MonthlyRent: 3200m,
            Meters: CorrectedReadings));

        Assert.Equal(3270m, preview.OldTotal.Amount);
        Assert.Equal(3800m, preview.NewTotal.Amount);
        Assert.Equal(3200m, preview.Revised.Rent.Amount);
        Assert.Equal(380m, preview.Revised.Water.Amount);
        Assert.Equal(120m, preview.Revised.Electricity.Amount);
        Assert.Equal(100m, preview.Revised.MotorcycleParking.Amount);
        Assert.Equal(MeterReading.Create(120m), preview.Revised.Meters!.CurrentWater);
        Assert.Equal(bill.InvoiceNumber, preview.Revised.InvoiceNumber);
        var stored = _repository.Get(bill.Id)!;
        Assert.Equal(3000m, stored.Rent.Amount);
        Assert.Equal(190m, stored.Water.Amount);
        Assert.Equal(0m, stored.MotorcycleParking.Amount);
        Assert.Equal(3270m, stored.Total.Amount);
        Assert.Equal(bill.Meters, stored.Meters);
    }

    [Fact]
    public void Preview_WhenBillIsPaid_IsRejected()
    {
        var bill = CreateStoredBill();
        new BillPaymentService(_repository).RecordPayment(bill.Id, new DateOnly(2026, 10, 3), PaymentMethod.Cash);
        var service = new UpdateBillChargesService(_repository);

        var exception = Assert.Throws<InvalidOperationException>(() => service.Preview(new UpdateBillChargesRequest(
            bill.Id, true, 100m, false, 500m, true, 60m, MonthlyRent: 3200m)));

        Assert.Equal("บิลนี้ชำระแล้วจึงแก้ไขไม่ได้ กรุณายกเลิกการรับชำระก่อนแก้ไข", exception.Message);
    }

    [Fact]
    public void Update_WhenBillDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new UpdateBillChargesService(_repository);

        Assert.Throws<InvalidOperationException>(() => service.Update(
            new UpdateBillChargesRequest(BillId.New(), true, 100m, false, 500m, true, 60m)));
    }
}
