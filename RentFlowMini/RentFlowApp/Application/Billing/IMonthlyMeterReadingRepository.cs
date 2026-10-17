using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public interface IMonthlyMeterReadingRepository
{
    void Add(MonthlyMeterReading reading);

    MonthlyMeterReading? Get(RoomId roomId, BillingPeriod billingPeriod);

    bool Exists(RoomId roomId, BillingPeriod billingPeriod);
}
