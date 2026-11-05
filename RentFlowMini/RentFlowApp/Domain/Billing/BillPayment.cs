namespace RentFlowApp.Domain.Billing;

public enum PaymentMethod
{
    Cash,
    BankTransfer
}

public sealed record BillPayment(DateOnly PaidOn, PaymentMethod Method)
{
    public string MethodName => Method == PaymentMethod.Cash ? "เงินสด" : "โอนเข้าบัญชี";
}
