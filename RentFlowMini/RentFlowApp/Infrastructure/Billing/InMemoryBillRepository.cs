using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class InMemoryBillRepository : IBillRepository
{
    private readonly List<Bill> _bills = [];

    public void Add(Bill bill)
    {
        ArgumentNullException.ThrowIfNull(bill);

        if (Exists(bill.RoomId, bill.BillingPeriod))
        {
            throw new InvalidOperationException(
                "มีบิลของห้องและรอบบิลนี้อยู่แล้ว");
        }

        _bills.Add(bill);
    }

    public bool Exists(RoomId roomId, BillingPeriod billingPeriod)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        return _bills.Exists(
            bill => bill.RoomId == roomId &&
                    bill.BillingPeriod == billingPeriod);
    }
}
