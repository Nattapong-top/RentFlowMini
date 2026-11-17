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
        // สร้างก่อนเขียนข้อมูล ถ้าเลขของรอบถัดไปใช้ไม่ได้จะได้ไม่มีรอบใดถูกบันทึกไปครึ่งเดียว
        var correctedNextReading = CreateNextPeriodCorrection(reading);

        bool isUpdate = _repository.Exists(roomId, billingPeriod);
        if (isUpdate)
        {
            _repository.Update(reading);
        }
        else
        {
            _repository.Add(reading);
        }

        var warnings = new List<string>();
        // บิลเก็บเลขมิเตอร์ไว้ตอนออกบิล และไม่เปลี่ยนเองจนกว่าผู้ใช้สั่งดึงข้อมูลห้องใหม่ ตาม ADR-003
        if (bill is not null)
        {
            warnings.Add(CreateStaleBillWarning(billingPeriod));
        }

        if (correctedNextReading is not null)
        {
            var nextPeriod = correctedNextReading.BillingPeriod;
            _repository.Update(correctedNextReading);
            warnings.Add(
                $"ปรับเลขก่อนหน้าของรอบ {ThaiCalendar.FormatPeriod(nextPeriod)} " +
                "ให้ตรงกับเลขปัจจุบันของรอบนี้แล้ว");
            if (_billRepository?.Get(roomId, nextPeriod) is not null)
            {
                warnings.Add(CreateStaleBillWarning(nextPeriod));
            }
        }

        return new MeterReadingSaveResult(
            warnings.Count == 0 ? null : string.Join(" ", warnings),
            isUpdate);
    }

    private static string CreateStaleBillWarning(BillingPeriod billingPeriod) =>
        $"บิลรอบ {ThaiCalendar.FormatPeriod(billingPeriod)} ของห้องนี้ออกแล้วและยังใช้เลขมิเตอร์เดิม " +
        "หากต้องการใช้เลขใหม่ กรุณาเปิดบิลในแท็บสร้างบิลประจำเดือนแล้วกด \"ดึงข้อมูลห้องใหม่\"";

    // เลขก่อนหน้าของรอบถัดไปคือสำเนาเลขปัจจุบันของรอบนี้ ถ้าไม่ปรับตาม เลขสองเดือนจะไม่ต่อกันและหน่วยช่วงนั้นจะไม่ถูกคิดเงิน
    private MonthlyMeterReading? CreateNextPeriodCorrection(MonthlyMeterReading reading)
    {
        var next = _repository.Get(reading.RoomId, reading.BillingPeriod.GetNext());
        if (next is null)
        {
            return null;
        }

        bool isAlreadyContinuous =
            next.PreviousWaterMeter.Value == reading.CurrentWaterMeter.Value &&
            next.PreviousElectricityMeter.Value == reading.CurrentElectricityMeter.Value;
        if (isAlreadyContinuous)
        {
            return null;
        }

        // เลขมิเตอร์ของบิลที่รับเงินแล้วห้ามเปลี่ยน และถ้าแก้เฉพาะรอบนี้ เลขสองเดือนจะไม่ต่อกัน จึงต้องปฏิเสธทั้งคู่
        if (_billRepository?.Get(next.RoomId, next.BillingPeriod) is { IsPaid: true })
        {
            string nextPeriodName = ThaiCalendar.FormatPeriod(next.BillingPeriod);
            throw new InvalidOperationException(
                $"บิลรอบ {nextPeriodName} ของห้องนี้ชำระแล้ว " +
                $"จึงแก้เลขปัจจุบันของรอบ {ThaiCalendar.FormatPeriod(reading.BillingPeriod)} ไม่ได้ " +
                $"หากต้องการแก้ไข กรุณายกเลิกการรับชำระของรอบ {nextPeriodName} ในแท็บสร้างบิลประจำเดือนก่อน");
        }

        EnsureNotAboveNextPeriod("มิเตอร์น้ำ", reading.CurrentWaterMeter, next.CurrentWaterMeter, next.BillingPeriod);
        EnsureNotAboveNextPeriod(
            "มิเตอร์ไฟฟ้า",
            reading.CurrentElectricityMeter,
            next.CurrentElectricityMeter,
            next.BillingPeriod);

        return MonthlyMeterReading.Create(
            next.RoomId,
            next.BillingPeriod,
            reading.CurrentWaterMeter,
            next.CurrentWaterMeter,
            reading.CurrentElectricityMeter,
            next.CurrentElectricityMeter);
    }

    // domain ปฏิเสธกรณีนี้อยู่แล้ว แต่ข้อความของ domain ไม่บอกว่าเลขที่ขัดกันอยู่ที่รอบถัดไป
    private static void EnsureNotAboveNextPeriod(
        string meterType,
        MeterReading current,
        MeterReading nextCurrent,
        BillingPeriod nextPeriod)
    {
        if (current.Value <= nextCurrent.Value)
        {
            return;
        }

        string nextPeriodName = ThaiCalendar.FormatPeriod(nextPeriod);
        throw new InvalidMeterReadingException(
            meterType,
            $"ค่าปัจจุบัน {current.Value:0.##} มากกว่าค่าปัจจุบันของรอบ {nextPeriodName} ({nextCurrent.Value:0.##}) " +
            $"กรุณาแก้เลขมิเตอร์ของรอบ {nextPeriodName} ก่อน");
    }
}
