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

    public BillMeterSnapshot? Meters { get; }

    public BillTenantSnapshot? Tenant { get; }

    public Money Rent { get; }

    public Money Water { get; }

    public Money Electricity { get; }

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

        if (IsPaid)
        {
            throw new InvalidOperationException(
                "บิลนี้ชำระแล้วจึงแก้ไขไม่ได้ กรุณายกเลิกการรับชำระก่อนแก้ไข");
        }

        MotorcycleParking = motorcycleParking;
        CarParking = carParking;
        CableTv = cableTv;
        IsCableTvExempt = isCableTvExempt;
        Total = Money.Create(
            Rent.Amount +
            Water.Amount +
            Electricity.Amount +
            motorcycleParking.Amount +
            carParking.Amount +
            cableTv.Amount);
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
