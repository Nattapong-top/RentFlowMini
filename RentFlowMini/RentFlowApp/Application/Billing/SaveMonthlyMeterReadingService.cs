using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;

namespace RentFlowApp.Application.Billing;

public sealed class SaveMonthlyMeterReadingService
{
    private readonly IMonthlyMeterReadingRepository _repository;

    public SaveMonthlyMeterReadingService(IMonthlyMeterReadingRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public void Save(
        RoomId roomId,
        BillingPeriod billingPeriod,
        MeterReading? previousWaterMeter,
        MeterReading? currentWaterMeter,
        MeterReading? previousElectricityMeter,
        MeterReading? currentElectricityMeter)
    {
        ArgumentNullException.ThrowIfNull(roomId);
        ArgumentNullException.ThrowIfNull(billingPeriod);

        var reading = MonthlyMeterReading.Create(
            roomId,
            billingPeriod,
            previousWaterMeter,
            currentWaterMeter,
            previousElectricityMeter,
            currentElectricityMeter);

        if (_repository.Exists(roomId, billingPeriod))
        {
            _repository.Update(reading);
        }
        else
        {
            _repository.Add(reading);
        }
    }
}
