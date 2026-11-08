using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class CreateMonthlyBillService
{
    private readonly IBillRepository _repository;

    public CreateMonthlyBillService(IBillRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public const string MissingMeterReadingMessage =
        "ยังไม่มีข้อมูลบันทึกมิเตอร์ของรอบบิลนี้ กรุณาบันทึกมิเตอร์น้ำ-ไฟก่อนทำการสร้างบิล";

    private const string NoTenantMessage = "ห้องนี้ไม่มีผู้เช่า จึงออกบิลไม่ได้ กรุณาเพิ่มผู้เช่าให้ห้องนี้ก่อน";

    // ร่างไม่บันทึกและไม่จองเลขใบแจ้งหนี้ ตอนออกบิลจริงต้องคำนวณใหม่ผ่าน Create เสมอ
    public MonthlyBillDraft Preview(CreateMonthlyBillRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Tenant is null)
        {
            return new MonthlyBillDraft(null, NoTenantMessage);
        }

        if (request.PreviousWaterMeter is null || request.CurrentWaterMeter is null ||
            request.PreviousElectricityMeter is null || request.CurrentElectricityMeter is null)
        {
            return new MonthlyBillDraft(null, MissingMeterReadingMessage);
        }

        try
        {
            return new MonthlyBillDraft(BuildBill(request, string.Empty), null);
        }
        catch (Exception ex) when (ex is InvalidMeterReadingException or ArgumentException)
        {
            return new MonthlyBillDraft(null, ex.Message);
        }
    }

    public Bill Create(CreateMonthlyBillRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // บิลต้องมีผู้รับผิดชอบเสมอ หน้าจอซ่อนห้องว่างไว้แล้ว แต่ตรวจซ้ำที่นี่เผื่อมีทางออกบิลทางอื่น
        if (request.Tenant is null)
        {
            throw new InvalidOperationException(NoTenantMessage);
        }

        var bill = BuildBill(request, NextInvoiceNumber(request.BillingPeriod));
        _repository.Add(bill);

        return bill;
    }

    // คำนวณเลขที่ใบแจ้งหนี้แบบเริ่มใหม่ที่ 001 ในทุกรอบเดือน (N001, N002, ...)
    private string NextInvoiceNumber(BillingPeriod billingPeriod)
    {
        int maxNum = 0;
        foreach (var b in _repository.GetAll().Where(b => b.BillingPeriod == billingPeriod))
        {
            if (b.InvoiceNumber.StartsWith('N') && int.TryParse(b.InvoiceNumber.AsSpan(1), out int num))
            {
                if (num > maxNum) maxNum = num;
            }
        }

        return $"N{(maxNum + 1):D3}";
    }

    private static Bill BuildBill(CreateMonthlyBillRequest request, string invoiceNumber)
    {
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

        return Bill.Create(
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
            request.Tenant,
            request.IsCableTvExempt);
    }
}
