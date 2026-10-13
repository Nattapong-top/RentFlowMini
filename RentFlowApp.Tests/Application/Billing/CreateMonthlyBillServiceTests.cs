using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Application.Billing;

public class CreateMonthlyBillServiceTests
{
    [Fact]
    public void Create_WithCompleteRequest_ReturnsMonthlyBill()
    {
        var service = new CreateMonthlyBillService();
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

        Assert.Equal(request.RoomId, bill.RoomId);
        Assert.Equal(request.BillingPeriod, bill.BillingPeriod);
        Assert.Equal(2990m, bill.Total.Amount);
    }
}
