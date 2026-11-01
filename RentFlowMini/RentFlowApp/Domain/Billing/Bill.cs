using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class Bill
{
    private Bill(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber,
        DateTime createdAt)
    {
        Id = BillId.New();
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

    public Money Rent { get; }

    public Money Water { get; }

    public Money Electricity { get; }

    public Money MotorcycleParking { get; }

    public Money CarParking { get; }

    public Money CableTv { get; }

    public Money Total { get; }

    public static Bill Create(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation,
        string invoiceNumber = "N001",
        DateTime? createdAt = null)
    {
        return new Bill(roomId, billingPeriod, calculation, invoiceNumber, createdAt ?? DateTime.Now);
    }
}
