using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Domain.Billing;

public sealed class Bill
{
    private Bill(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation)
    {
        Id = BillId.New();
        RoomId = roomId;
        BillingPeriod = billingPeriod;
        Rent = calculation.Rent;
        Water = calculation.Water;
        Electricity = calculation.Electricity;
        Total = calculation.Total;
    }

    public BillId Id { get; }

    public RoomId RoomId { get; }

    public BillingPeriod BillingPeriod { get; }

    public Money Rent { get; }

    public Money Water { get; }

    public Money Electricity { get; }

    public Money Total { get; }

    public static Bill Create(
        RoomId roomId,
        BillingPeriod billingPeriod,
        BillingCalculationResult calculation)
    {
        return new Bill(roomId, billingPeriod, calculation);
    }
}
