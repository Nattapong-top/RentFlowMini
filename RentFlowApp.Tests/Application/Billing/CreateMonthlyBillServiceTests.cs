using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class CreateMonthlyBillServiceTests
{
    [Fact]
    public void Create_WithCompleteRequest_ReturnsMonthlyBill()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 2800m,
            PreviousWaterMeter: MeterReading.Create(120m),
            CurrentWaterMeter: MeterReading.Create(125m),
            PreviousElectricityMeter: MeterReading.Create(1000m),
            CurrentElectricityMeter: MeterReading.Create(1010m),
            WaterRate: 30m,
            ElectricityRate: 4m);

        var bill = service.Create(request);

        Assert.NotEqual(Guid.Empty, bill.Id.Value);
        Assert.Equal(request.RoomId, bill.RoomId);
        Assert.Equal(request.BillingPeriod, bill.BillingPeriod);
        Assert.Equal(2990m, bill.Total.Amount);
        Assert.True(
            repository.Exists(request.RoomId, request.BillingPeriod));
    }

    [Fact]
    public void Create_WithParkingAndCableTv_ReturnsBillWithAllCharges()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 3000m,
            PreviousWaterMeter: MeterReading.Create(100m),
            CurrentWaterMeter: MeterReading.Create(110m),
            PreviousElectricityMeter: MeterReading.Create(500m),
            CurrentElectricityMeter: MeterReading.Create(550m),
            WaterRate: 19m,
            ElectricityRate: 8m,
            ChargeMotorcycleParking: true,
            MotorcycleParkingRate: 300m,
            ChargeCarParking: true,
            CarParkingRate: 500m,
            IsCableTvExempt: false,
            CableTvRate: 60m);

        var bill = service.Create(request);

        Assert.Equal(3000m, bill.Rent.Amount);
        Assert.Equal(190m, bill.Water.Amount);
        Assert.Equal(400m, bill.Electricity.Amount);
        Assert.Equal(300m, bill.MotorcycleParking.Amount);
        Assert.Equal(500m, bill.CarParking.Amount);
        Assert.Equal(60m, bill.CableTv.Amount);
        Assert.Equal(4450m, bill.Total.Amount);
    }

    [Fact]
    public void Create_WhenRoomAndPeriodAlreadyHaveBill_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryBillRepository();
        var service = new CreateMonthlyBillService(repository);
        var request = new CreateMonthlyBillRequest(
            RoomId: RoomId.New(),
            BillingPeriod: BillingPeriod.Create("2026-09"),
            MonthlyRent: 2800m,
            PreviousWaterMeter: MeterReading.Create(120m),
            CurrentWaterMeter: MeterReading.Create(125m),
            PreviousElectricityMeter: MeterReading.Create(1000m),
            CurrentElectricityMeter: MeterReading.Create(1010m),
            WaterRate: 30m,
            ElectricityRate: 4m);

        service.Create(request);

        Assert.Throws<InvalidOperationException>(() =>
            service.Create(request));
    }
}
