using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillTests
{
    [Fact]
    public void Restore_KeepsEverySavedValue()
    {
        var billId = BillId.New();
        var roomId = RoomId.New();
        var billingPeriod = BillingPeriod.Create("2026-09");
        var createdAt = new DateTime(2026, 9, 30, 14, 15, 16);
        var savedDueDate = new DateOnly(2026, 12, 31);
        var meters = new BillMeterSnapshot(
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(1000m),
            MeterReading.Create(1010m),
            6m,
            4m);
        var tenant = new BillTenantSnapshot(TenantId.New(), "Anna Example");
        var payment = new BillPayment(new DateOnly(2026, 10, 3), PaymentMethod.BankTransfer);

        var bill = Bill.Restore(
            billId,
            roomId,
            billingPeriod,
            new BillingCalculationResult(
                Rent: Money.Create(2800.25m),
                Water: Money.Create(150.50m),
                Electricity: Money.Create(40.75m),
                MotorcycleParking: Money.Create(300m),
                CarParking: Money.Create(500m),
                CableTv: Money.Create(0m),
                Total: Money.Create(3791.50m)),
            "N007",
            createdAt,
            savedDueDate,
            meters,
            tenant,
            isCableTvExempt: true,
            payment);

        Assert.Equal(billId, bill.Id);
        Assert.Equal(roomId, bill.RoomId);
        Assert.Equal(billingPeriod, bill.BillingPeriod);
        Assert.Equal(2800.25m, bill.Rent.Amount);
        Assert.Equal(150.50m, bill.Water.Amount);
        Assert.Equal(40.75m, bill.Electricity.Amount);
        Assert.Equal(300m, bill.MotorcycleParking.Amount);
        Assert.Equal(500m, bill.CarParking.Amount);
        Assert.Equal(0m, bill.CableTv.Amount);
        Assert.Equal(3791.50m, bill.Total.Amount);
        Assert.Equal("N007", bill.InvoiceNumber);
        Assert.Equal(createdAt, bill.CreatedAt);
        Assert.Equal(savedDueDate, bill.DueDate);
        Assert.Equal(meters, bill.Meters);
        Assert.Equal(tenant, bill.Tenant);
        Assert.True(bill.IsCableTvExempt);
        Assert.Equal(payment, bill.Payment);
    }

    [Fact]
    public void Create_WithRoomAndBillingPeriod_CreatesBillFromCalculation()
    {
        var roomId = RoomId.New();
        var calculationWithTotal = new BillingCalculationResult(
            Rent: Money.Create(2800m),
            Water: Money.Create(150m),
            Electricity: Money.Create(40m),
            MotorcycleParking: Money.Create(300m),
            CarParking: Money.Create(500m),
            CableTv: Money.Create(60m),
            Total: Money.Create(3850m));
        var billingPeriod = BillingPeriod.Create("2026-09");

        var bill = Bill.Create(roomId, billingPeriod, calculationWithTotal);

        Assert.NotEqual(Guid.Empty, bill.Id.Value);
        Assert.Equal(roomId, bill.RoomId);
        Assert.Equal(billingPeriod, bill.BillingPeriod);
        Assert.Equal(2800m, bill.Rent.Amount);
        Assert.Equal(150m, bill.Water.Amount);
        Assert.Equal(40m, bill.Electricity.Amount);
        Assert.Equal(300m, bill.MotorcycleParking.Amount);
        Assert.Equal(500m, bill.CarParking.Amount);
        Assert.Equal(60m, bill.CableTv.Amount);
        Assert.Equal(3850m, bill.Total.Amount);
    }

    [Fact]
    public void Create_WithMeterSnapshot_KeepsMetersAndSetsDueDateFromBillingPeriod()
    {
        var calculation = new BillingCalculationResult(
            Rent: Money.Create(2800m),
            Water: Money.Create(150m),
            Electricity: Money.Create(40m),
            MotorcycleParking: Money.Create(0m),
            CarParking: Money.Create(0m),
            CableTv: Money.Create(0m),
            Total: Money.Create(2990m));
        var meters = new BillMeterSnapshot(
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(505m),
            WaterRate: 15m,
            ElectricityRate: 8m);

        var bill = Bill.Create(RoomId.New(), BillingPeriod.Create("2026-09"), calculation, meters: meters);

        Assert.Equal(meters, bill.Meters);
        Assert.Equal(10m, bill.Meters!.WaterUsage);
        Assert.Equal(5m, bill.Meters.ElectricityUsage);
        Assert.Equal(new DateOnly(2026, 10, 5), bill.DueDate);
    }
}
