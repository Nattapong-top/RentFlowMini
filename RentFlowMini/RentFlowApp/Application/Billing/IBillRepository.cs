using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public interface IBillRepository
{
    void Add(Bill bill);

    bool Exists(RoomId roomId, BillingPeriod billingPeriod);
}
