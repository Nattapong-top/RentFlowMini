using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class UpdateBillChargesService
{
    private readonly IBillRepository _repository;

    public UpdateBillChargesService(IBillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Bill Update(UpdateBillChargesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bill = GetBill(request.BillId);
        Apply(bill, request);
        _repository.Update(bill);

        return bill;
    }

    // คำนวณบนสำเนา เพราะ repository บางตัวคืนบิลตัวเดียวกับที่เก็บไว้ ถ้าแก้ตัวจริงจะเท่ากับบันทึกไปแล้ว
    public BillRevisionPreview Preview(UpdateBillChargesRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var bill = GetBill(request.BillId);
        var revised = Copy(bill);
        Apply(revised, request);

        return new BillRevisionPreview(bill.Total, revised.Total, revised);
    }

    private Bill GetBill(BillId billId) =>
        _repository.Get(billId)
            ?? throw new InvalidOperationException("ไม่พบบิลที่ต้องการแก้ไข กรุณาเลือกบิลจากตารางอีกครั้ง");

    private static void Apply(Bill bill, UpdateBillChargesRequest request)
    {
        if (request.MonthlyRent is not null || request.Meters is not null)
        {
            bill.ReviseRoomCharges(Money.Create(request.MonthlyRent ?? bill.Rent.Amount), request.Meters);
        }

        bill.UpdateAdditionalCharges(
            ResolveCharge(request.ChargeMotorcycleParking, bill.MotorcycleParking, request.MotorcycleParkingRate),
            ResolveCharge(request.ChargeCarParking, bill.CarParking, request.CarParkingRate),
            ResolveCableTvCharge(bill, request),
            request.IsCableTvExempt);
    }

    private static Bill Copy(Bill bill) =>
        Bill.Restore(
            bill.Id,
            bill.RoomId,
            bill.BillingPeriod,
            new BillingCalculationResult(
                bill.Rent,
                bill.Water,
                bill.Electricity,
                bill.MotorcycleParking,
                bill.CarParking,
                bill.CableTv,
                bill.Total),
            bill.InvoiceNumber,
            bill.CreatedAt,
            bill.DueDate,
            bill.Meters,
            bill.Tenant,
            bill.IsCableTvExempt,
            bill.Payment);

    // บิลที่ไม่ได้ยกเว้นคิดเงินอยู่แล้ว จึงคงยอดเดิมแม้เป็น 0 จากราคากลางตอนออกบิล
    private static Money ResolveCableTvCharge(Bill bill, UpdateBillChargesRequest request)
    {
        if (request.IsCableTvExempt)
        {
            return Money.Create(0m);
        }

        return bill.IsCableTvExempt ? Money.Create(request.CableTvRate) : bill.CableTv;
    }

    // ยอดในบิลเป็น snapshot: รายการที่คิดเงินอยู่แล้วต้องคงยอดเดิม แม้ราคากลางจะเปลี่ยนหลังออกบิล
    private static Money ResolveCharge(bool isCharged, Money currentAmount, decimal rate)
    {
        if (!isCharged)
        {
            return Money.Create(0m);
        }

        return currentAmount.Amount > 0m ? currentAmount : Money.Create(rate);
    }
}
