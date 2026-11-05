using RentFlowApp.Domain.Billing;

namespace RentFlowApp.Application.Billing;

public sealed class CreateMonthlyBillService
{
    private readonly IBillRepository _repository;

    public CreateMonthlyBillService(IBillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public Bill Create(CreateMonthlyBillRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var calculation = BillingCalculator.Calculate(
            monthlyRent: request.MonthlyRent,
            previousWaterMeter: request.PreviousWaterMeter,
            currentWaterMeter: request.CurrentWaterMeter,
            previousElectricityMeter: request.PreviousElectricityMeter,
            currentElectricityMeter: request.CurrentElectricityMeter,
            waterRate: request.WaterRate,
            electricityRate: request.ElectricityRate,
            chargeMotorcycleParking: request.ChargeMotorcycleParking,
            motorcycleParkingRate: request.MotorcycleParkingRate,
            chargeCarParking: request.ChargeCarParking,
            carParkingRate: request.CarParkingRate,
            isCableTvExempt: request.IsCableTvExempt,
            cableTvRate: request.CableTvRate);

        // คำนวณเลขที่ใบแจ้งหนี้แบบเริ่มใหม่ที่ 001 ในทุกรอบเดือน (N001, N002, ...)
        int maxNum = 0;
        foreach (var b in _repository.GetAll().Where(b => b.BillingPeriod == request.BillingPeriod))
        {
            if (b.InvoiceNumber.StartsWith('N') && int.TryParse(b.InvoiceNumber.AsSpan(1), out int num))
            {
                if (num > maxNum) maxNum = num;
            }
        }
        string invoiceNumber = $"N{(maxNum + 1):D3}";

        var bill = Bill.Create(
            request.RoomId,
            request.BillingPeriod,
            calculation,
            invoiceNumber,
            DateTime.Now,
            // BillingCalculator ปฏิเสธมิเตอร์ที่เป็น null ไปแล้วด้านบน
            new BillMeterSnapshot(
                request.PreviousWaterMeter!,
                request.CurrentWaterMeter!,
                request.PreviousElectricityMeter!,
                request.CurrentElectricityMeter!,
                request.WaterRate,
                request.ElectricityRate),
            request.Tenant);

        _repository.Add(bill);

        return bill;
    }
}
