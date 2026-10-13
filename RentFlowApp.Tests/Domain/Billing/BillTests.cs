using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillTests
{
    [Fact]
    public void Create_WithRoomAndBillingPeriod_CreatesBillFromCalculation()
    {
        var roomId = RoomId.New();
        var calculation = new BillingCalculationResult(
            Rent: Money.Create(2800m),
            Water: Money.Create(150m),
            Electricity: Money.Create(40m),
            Total: Money.Create(2990m));
        var billingPeriod = BillingPeriod.Create("2026-09");

        var bill = Bill.Create(roomId, billingPeriod, calculation);

        Assert.NotEqual(Guid.Empty, bill.Id.Value);
        Assert.Equal(roomId, bill.RoomId);
        Assert.Equal(billingPeriod, bill.BillingPeriod);
        Assert.Equal(2800m, bill.Rent.Amount);
        Assert.Equal(150m, bill.Water.Amount);
        Assert.Equal(40m, bill.Electricity.Amount);
        Assert.Equal(2990m, bill.Total.Amount);
    }

}
