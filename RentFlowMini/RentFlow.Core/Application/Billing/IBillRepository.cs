using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public interface IBillRepository
{
    void Add(Bill bill);

    void Update(Bill bill);

    bool Exists(RoomId roomId, BillingPeriod billingPeriod);

    IReadOnlyCollection<Bill> GetAll();

    Bill? Get(BillId id);
}
