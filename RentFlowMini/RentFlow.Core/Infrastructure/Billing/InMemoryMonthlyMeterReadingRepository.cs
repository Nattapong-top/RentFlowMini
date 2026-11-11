using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Infrastructure.Billing;

public sealed class InMemoryMonthlyMeterReadingRepository : IMonthlyMeterReadingRepository
{
    private readonly List<MonthlyMeterReading> _readings = [];

    public void Add(MonthlyMeterReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        if (Exists(reading.RoomId, reading.BillingPeriod))
        {
            throw new InvalidOperationException(
                "มีข้อมูลมิเตอร์ของห้องและรอบบิลนี้อยู่แล้ว");
        }

        _readings.Add(reading);
    }

    public void Update(MonthlyMeterReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var existing = Get(reading.RoomId, reading.BillingPeriod);
        if (existing is not null)
        {
            _readings.Remove(existing);
        }

        _readings.Add(reading);
    }

    public MonthlyMeterReading? Get(
        RoomId roomId,
        BillingPeriod billingPeriod)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        return _readings.FirstOrDefault(
            reading => reading.RoomId == roomId &&
                       reading.BillingPeriod == billingPeriod);
    }

    public bool Exists(RoomId roomId, BillingPeriod billingPeriod)
    {
        return Get(roomId, billingPeriod) is not null;
    }

    public IEnumerable<MonthlyMeterReading> GetAll()
    {
        return _readings.AsReadOnly();
    }
}
