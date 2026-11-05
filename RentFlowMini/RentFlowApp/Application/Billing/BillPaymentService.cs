using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class BillPaymentService
{
    private readonly IBillRepository _repository;

    public BillPaymentService(IBillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Bill RecordPayment(BillId billId, DateOnly paidOn, PaymentMethod method)
    {
        if (paidOn > DateOnly.FromDateTime(DateTime.Now))
        {
            throw new InvalidOperationException("วันที่รับเงินต้องไม่เกินวันนี้");
        }

        var bill = GetBill(billId);
        bill.RecordPayment(paidOn, method);
        _repository.Update(bill);

        return bill;
    }

    public Bill CancelPayment(BillId billId)
    {
        var bill = GetBill(billId);
        bill.CancelPayment();
        _repository.Update(bill);

        return bill;
    }

    private Bill GetBill(BillId billId) =>
        _repository.Get(billId)
        ?? throw new InvalidOperationException("ไม่พบบิลที่เลือก กรุณาเลือกบิลจากตารางอีกครั้ง");
}
