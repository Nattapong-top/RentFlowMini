using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Infrastructure.Billing;

public class InMemoryBillRepositoryTests
{
    [Fact]
    public void Exists_WithStoredBillForRoomAndPeriod_ReturnsTrue()
    {
        var repository = new InMemoryBillRepository();
        var roomId = RoomId.New();
        var billingPeriod = BillingPeriod.Create("2026-09");
        var calculation = new BillingCalculationResult(
            Money.Create(2800m),
            Money.Create(150m),
            Money.Create(40m),
            Money.Create(2990m));
        var bill = Bill.Create(roomId, billingPeriod, calculation);

        repository.Add(bill);

        Assert.True(repository.Exists(roomId, billingPeriod));
    }

    [Fact]
    public void Add_WhenRoomAndPeriodAlreadyExist_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryBillRepository();
        var roomId = RoomId.New();
        var billingPeriod = BillingPeriod.Create("2026-09");
        var calculation = new BillingCalculationResult(
            Money.Create(2800m),
            Money.Create(150m),
            Money.Create(40m),
            Money.Create(2990m));
        var firstBill = Bill.Create(roomId, billingPeriod, calculation);
        var duplicateBill = Bill.Create(roomId, billingPeriod, calculation);

        repository.Add(firstBill);

        Assert.Throws<InvalidOperationException>(() =>
            repository.Add(duplicateBill));
    }
}
