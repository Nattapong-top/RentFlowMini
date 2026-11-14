using RentFlowApp.Application.Reports;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class SaveMonthlyMeterReadingService
{
    private readonly IMonthlyMeterReadingRepository _repository;
    private readonly IBillRepository? _billRepository;

    public SaveMonthlyMeterReadingService(
        IMonthlyMeterReadingRepository repository,
        IBillRepository? billRepository = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
        _billRepository = billRepository;
    }

    public MeterReadingSaveResult Save(
        RoomId roomId,
        BillingPeriod billingPeriod,
        MeterReading? previousWaterMeter,
        MeterReading? currentWaterMeter,
        MeterReading? previousElectricityMeter,
        MeterReading? currentElectricityMeter)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        var bill = _billRepository?.Get(roomId, billingPeriod);
        // บิลที่ชำระแล้วคือยอดที่ผู้เช่าจ่ายไปแล้ว ถ้าแก้มิเตอร์ได้ เลขในระบบจะไม่ตรงกับบิลที่รับเงินมา
        if (bill is { IsPaid: true })
        {
            throw new InvalidOperationException(
                $"บิลรอบ {ThaiCalendar.FormatPeriod(billingPeriod)} ของห้องนี้ชำระแล้ว จึงแก้เลขมิเตอร์ไม่ได้ " +
                "หากต้องการแก้ไข กรุณายกเลิกการรับชำระในแท็บสร้างบิลประจำเดือนก่อน");
        }

        var reading = MonthlyMeterReading.Create(
            roomId,
            billingPeriod,
            previousWaterMeter,
            currentWaterMeter,
            previousElectricityMeter,
            currentElectricityMeter);

        if (_repository.Exists(roomId, billingPeriod))
        {
            _repository.Update(reading);
        }
        else
        {
            _repository.Add(reading);
        }

        // บิลเก็บเลขมิเตอร์ไว้ตอนออกบิล และไม่เปลี่ยนเองจนกว่าผู้ใช้สั่งดึงข้อมูลห้องใหม่ ตาม ADR-003
        return bill is null
            ? new MeterReadingSaveResult(null)
            : new MeterReadingSaveResult(
                $"บิลรอบ {ThaiCalendar.FormatPeriod(billingPeriod)} ของห้องนี้ออกแล้วและยังใช้เลขมิเตอร์เดิม " +
                "หากต้องการใช้เลขใหม่ กรุณาเปิดบิลในแท็บสร้างบิลประจำเดือนแล้วกด \"ดึงข้อมูลห้องใหม่\"");
    }
}
