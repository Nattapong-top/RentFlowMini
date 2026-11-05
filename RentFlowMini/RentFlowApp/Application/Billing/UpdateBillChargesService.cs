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

        var bill = _repository.Get(request.BillId)
            ?? throw new InvalidOperationException("ไม่พบบิลที่ต้องการแก้ไข กรุณาเลือกบิลจากตารางอีกครั้ง");

        bill.UpdateAdditionalCharges(
            ResolveCharge(request.ChargeMotorcycleParking, bill.MotorcycleParking, request.MotorcycleParkingRate),
            ResolveCharge(request.ChargeCarParking, bill.CarParking, request.CarParkingRate),
            ResolveCharge(!request.IsCableTvExempt, bill.CableTv, request.CableTvRate));

        _repository.Update(bill);

        return bill;
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
