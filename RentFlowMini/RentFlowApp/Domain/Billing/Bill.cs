using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class Bill
{
    private Bill(
        BillId id,
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber,
        DateTime createdAt,
        DateOnly dueDate,
        BillMeterSnapshot? meters,
        BillTenantSnapshot? tenant,
        bool isCableTvExempt,
        BillPayment? payment)
    {
        Id = id;
        IsCableTvExempt = isCableTvExempt;
        Meters = meters;
        Tenant = tenant;
        Payment = payment;
        DueDate = dueDate;
        RoomId = roomId;
        BillingPeriod = billingPeriod;
        InvoiceNumber = invoiceNumber;
        CreatedAt = createdAt;
        Rent = calculation.Rent;
        Water = calculation.Water;
        Electricity = calculation.Electricity;
        MotorcycleParking = calculation.MotorcycleParking;
        CarParking = calculation.CarParking;
        CableTv = calculation.CableTv;
        Total = calculation.Total;
    }

    public BillId Id { get; }

    public RoomId RoomId { get; }

    public BillingPeriod BillingPeriod { get; }

    public string InvoiceNumber { get; }

    public DateTime CreatedAt { get; }

    public DateOnly DueDate { get; }

    public const string PaidBillCannotBeChangedMessage =
        "บิลนี้ชำระแล้วจึงแก้ไขไม่ได้ กรุณายกเลิกการรับชำระก่อนแก้ไข";

    public BillMeterSnapshot? Meters { get; private set; }

    public BillTenantSnapshot? Tenant { get; }

    public Money Rent { get; private set; }

    public Money Water { get; private set; }

    public Money Electricity { get; private set; }

    public Money MotorcycleParking { get; private set; }

    public Money CarParking { get; private set; }

    public Money CableTv { get; private set; }

    public Money Total { get; private set; }

    // เก็บแยกจากยอดเงิน เพราะยอด 0 อาจมาจากราคากลางเป็น 0 ไม่ใช่การยกเว้น
    public bool IsCableTvExempt { get; private set; }

    public BillPayment? Payment { get; private set; }

    public bool IsPaid => Payment is not null;

    // วันครบกำหนดยังจ่ายทัน จึงเริ่มเกินกำหนดตั้งแต่วันถัดไป
    public bool IsOverdue(DateOnly today) => !IsPaid && today > DueDate;

    public int GetOverdueDays(DateOnly today) =>
        IsOverdue(today) ? today.DayNumber - DueDate.DayNumber : 0;

    public void RecordPayment(DateOnly paidOn, PaymentMethod method)
    {
        if (IsPaid)
        {
            throw new InvalidOperationException("บิลนี้บันทึกรับชำระแล้ว");
        }

        Payment = new BillPayment(paidOn, method);
    }

    public void CancelPayment()
    {
        if (!IsPaid)
        {
            throw new InvalidOperationException("บิลนี้ยังไม่ได้บันทึกรับชำระ");
        }

        Payment = null;
    }

    public void UpdateAdditionalCharges(
        Money motorcycleParking,
        Money carParking,
        Money cableTv,
        bool isCableTvExempt = false)
    {
        ArgumentNullException.ThrowIfNull(motorcycleParking);
        ArgumentNullException.ThrowIfNull(carParking);
        ArgumentNullException.ThrowIfNull(cableTv);

        EnsureNotPaid();

        MotorcycleParking = motorcycleParking;
        CarParking = carParking;
        CableTv = cableTv;
        IsCableTvExempt = isCableTvExempt;
        RecalculateTotal();
    }

    // ใช้แก้บิลที่ออกผิดหลังแก้ข้อมูลต้นทางแล้ว อัตราค่าน้ำค่าไฟคงตามที่บิลเก็บไว้ ไม่ใช้อัตราปัจจุบัน
    public void ReviseRoomCharges(Money rent, BillMeterReadings? readings)
    {
        ArgumentNullException.ThrowIfNull(rent);
        EnsureNotPaid();

        var meters = Meters;
        var water = Water;
        var electricity = Electricity;

        if (readings is not null)
        {
            if (Meters is null)
            {
                throw new InvalidOperationException(
                    "บิลนี้ไม่ได้เก็บอัตราค่าน้ำและค่าไฟไว้ จึงคำนวณค่าน้ำค่าไฟใหม่ไม่ได้");
            }

            var calculation = BillingCalculator.Calculate(
                rent.Amount,
                readings.PreviousWater,
                readings.CurrentWater,
                readings.PreviousElectricity,
                readings.CurrentElectricity,
                Meters.WaterRate,
                Meters.ElectricityRate);
            water = calculation.Water;
            electricity = calculation.Electricity;
            meters = Meters with
            {
                PreviousWater = readings.PreviousWater,
                CurrentWater = readings.CurrentWater,
                PreviousElectricity = readings.PreviousElectricity,
                CurrentElectricity = readings.CurrentElectricity
            };
        }

        Rent = rent;
        Water = water;
        Electricity = electricity;
        Meters = meters;
        RecalculateTotal();
    }

    private void EnsureNotPaid()
    {
        if (IsPaid)
        {
            throw new InvalidOperationException(PaidBillCannotBeChangedMessage);
        }
    }

    private void RecalculateTotal()
    {
        Total = Money.Create(
            Rent.Amount +
            Water.Amount +
            Electricity.Amount +
            MotorcycleParking.Amount +
            CarParking.Amount +
            CableTv.Amount);
    }

    public static Bill Create(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber = "N001",
        DateTime? createdAt = null,
        BillMeterSnapshot? meters = null,
        BillTenantSnapshot? tenant = null,
        bool isCableTvExempt = false)
    {
        return new Bill(
            BillId.New(),
            roomId,
            billingPeriod,
            calculation,
            invoiceNumber,
            createdAt ?? DateTime.Now,
            billingPeriod.GetPaymentDueDate(),
            meters,
            tenant,
            isCableTvExempt,
            payment: null);
    }

    // ใช้ค่าที่บันทึกไว้ ไม่คำนวณใหม่ เพื่อให้บิลเก่าไม่เปลี่ยนแม้กติกาการคำนวณจะเปลี่ยนภายหลัง
    public static Bill Restore(
        BillId id,
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber,
        DateTime createdAt,
        DateOnly dueDate,
        BillMeterSnapshot? meters,
        BillTenantSnapshot? tenant,
        bool isCableTvExempt,
        BillPayment? payment)
    {
        return new Bill(
            id,
            roomId,
            billingPeriod,
            calculation,
            invoiceNumber,
            createdAt,
            dueDate,
            meters,
            tenant,
            isCableTvExempt,
            payment);
    }
}
