using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillTests
{
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
