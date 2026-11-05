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
            60m));
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

    [Fact]
    public void Update_WhenBillDoesNotExist_ThrowsInvalidOperationException()
    {
        var service = new UpdateBillChargesService(_repository);

        Assert.Throws<InvalidOperationException>(() => service.Update(
            new UpdateBillChargesRequest(BillId.New(), true, 100m, false, 500m, true, 60m)));
    }
}
