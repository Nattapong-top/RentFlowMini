using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Domain.Billing;

public class BillOverdueTests
{
    private static Bill CreateBillDueOn5Nov2026()
    {
        var meter = MeterReading.Create(100m);

        return new CreateMonthlyBillService(new InMemoryBillRepository()).Create(new CreateMonthlyBillRequest(
            RoomId.New(), BillingPeriod.Create("2026-10"), 3000m, meter, meter, meter, meter, 19m, 8m));
    }

    [Theory]
    [InlineData(2026, 11, 4, false, 0)]
    [InlineData(2026, 11, 5, false, 0)]
    [InlineData(2026, 11, 6, true, 1)]
    [InlineData(2027, 1, 5, true, 61)]
    public void IsOverdue_StartsTheDayAfterDueDate(int year, int month, int day, bool expectedOverdue, int expectedDays)
    {
        var bill = CreateBillDueOn5Nov2026();
        var today = new DateOnly(year, month, day);

        Assert.Equal(expectedOverdue, bill.IsOverdue(today));
        Assert.Equal(expectedDays, bill.GetOverdueDays(today));
    }

    [Fact]
    public void IsOverdue_WhenPaid_IsFalseEvenIfPaidAfterDueDate()
    {
        var bill = CreateBillDueOn5Nov2026();
        var today = new DateOnly(2026, 12, 1);

        bill.RecordPayment(today, PaymentMethod.Cash);

        Assert.False(bill.IsOverdue(today));
        Assert.Equal(0, bill.GetOverdueDays(today));
    }

    [Fact]
    public void IsOverdue_AfterPaymentIsCancelled_CountsAgain()
    {
        var bill = CreateBillDueOn5Nov2026();
        var today = new DateOnly(2026, 11, 8);
        bill.RecordPayment(today, PaymentMethod.Cash);

        bill.CancelPayment();

        Assert.True(bill.IsOverdue(today));
        Assert.Equal(3, bill.GetOverdueDays(today));
    }
}
