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
