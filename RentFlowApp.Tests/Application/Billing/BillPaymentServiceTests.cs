using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class BillPaymentServiceTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Now);

    private readonly InMemoryBillRepository _repository = new();

    private BillPaymentService CreateService() => new(_repository);

    private Bill CreateStoredBill()
    {
        var meter = MeterReading.Create(100m);

        return new CreateMonthlyBillService(_repository).Create(new CreateMonthlyBillRequest(
            RoomId.New(), BillingPeriod.Create("2026-09"), 3000m, meter, meter, meter, meter, 19m, 8m));
    }

    [Fact]
    public void RecordPayment_StoresDateAndMethodWithoutChangingAmounts()
    {
        var bill = CreateStoredBill();
        decimal total = bill.Total.Amount;

        var paid = CreateService().RecordPayment(bill.Id, Today, PaymentMethod.BankTransfer);

        Assert.True(paid.IsPaid);
        Assert.Equal(Today, paid.Payment!.PaidOn);
        Assert.Equal(PaymentMethod.BankTransfer, paid.Payment.Method);
        Assert.Equal("โอนเข้าบัญชี", paid.Payment.MethodName);
        Assert.Equal(total, paid.Total.Amount);
    }

    [Fact]
    public void RecordPayment_WhenAlreadyPaid_KeepsFirstPayment()
    {
        var bill = CreateStoredBill();
        var service = CreateService();
        service.RecordPayment(bill.Id, Today, PaymentMethod.Cash);

        Assert.Throws<InvalidOperationException>(() =>
            service.RecordPayment(bill.Id, Today.AddDays(-1), PaymentMethod.BankTransfer));

        Assert.Equal(Today, bill.Payment!.PaidOn);
        Assert.Equal(PaymentMethod.Cash, bill.Payment.Method);
    }

    [Fact]
    public void RecordPayment_WhenDateIsInFuture_IsRejected()
    {
        var bill = CreateStoredBill();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            CreateService().RecordPayment(bill.Id, Today.AddDays(1), PaymentMethod.Cash));

        Assert.Contains("ต้องไม่เกินวันนี้", exception.Message);
        Assert.False(bill.IsPaid);
    }

    [Fact]
    public void CancelPayment_MakesBillUnpaidAndEditableAgain()
    {
        var bill = CreateStoredBill();
        var service = CreateService();
        service.RecordPayment(bill.Id, Today, PaymentMethod.Cash);

        service.CancelPayment(bill.Id);

        Assert.False(bill.IsPaid);
        Assert.Null(bill.Payment);
        new UpdateBillChargesService(_repository).Update(new UpdateBillChargesRequest(
            bill.Id, true, 100m, false, 500m, true, 60m));
        Assert.Equal(100m, bill.MotorcycleParking.Amount);
    }

    [Fact]
    public void CancelPayment_WhenBillIsUnpaid_IsRejected()
    {
        var bill = CreateStoredBill();

        Assert.Throws<InvalidOperationException>(() => CreateService().CancelPayment(bill.Id));
    }

    [Fact]
    public void Payment_WhenBillDoesNotExist_IsRejected()
    {
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() =>
            service.RecordPayment(BillId.New(), Today, PaymentMethod.Cash));
        Assert.Throws<InvalidOperationException>(() => service.CancelPayment(BillId.New()));
    }

    [Fact]
    public void UpdateCharges_WhenBillIsPaid_IsRejectedAndKeepsAmounts()
    {
        var bill = CreateStoredBill();
        CreateService().RecordPayment(bill.Id, Today, PaymentMethod.Cash);
        decimal total = bill.Total.Amount;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new UpdateBillChargesService(_repository).Update(new UpdateBillChargesRequest(
                bill.Id, true, 100m, false, 500m, true, 60m)));

        Assert.Contains("ยกเลิกการรับชำระ", exception.Message);
        Assert.Equal(total, bill.Total.Amount);
    }
}
