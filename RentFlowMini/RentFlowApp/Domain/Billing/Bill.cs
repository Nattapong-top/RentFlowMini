using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class Bill
{
    private Bill(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber,
        DateTime createdAt,
        BillMeterSnapshot? meters)
    {
        Id = BillId.New();
        Meters = meters;
        DueDate = billingPeriod.GetPaymentDueDate();
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

    public Money Rent { get; }

    public Money Water { get; }

    public Money Electricity { get; }

    public Money MotorcycleParking { get; private set; }

    public Money CarParking { get; private set; }

    public Money CableTv { get; private set; }

    public Money Total { get; private set; }

    public BillPayment? Payment { get; private set; }

    public bool IsPaid => Payment is not null;

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

    public void UpdateAdditionalCharges(Money motorcycleParking, Money carParking, Money cableTv)
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
        BillMeterSnapshot? meters = null)
    {
        return new Bill(roomId, billingPeriod, calculation, invoiceNumber, createdAt ?? DateTime.Now, meters);
    }
}
