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

    public void UpdateAdditionalCharges(Money motorcycleParking, Money carParking, Money cableTv)
    {
        ArgumentNullException.ThrowIfNull(motorcycleParking);
        ArgumentNullException.ThrowIfNull(carParking);
        ArgumentNullException.ThrowIfNull(cableTv);

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
