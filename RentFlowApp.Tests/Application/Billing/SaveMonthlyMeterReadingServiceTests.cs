using RentFlowApp.Application.Billing;
using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Application.Billing;

public class SaveMonthlyMeterReadingServiceTests
{
    [Fact]
    public void Save_WhenValidReading_SavesToRepository()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

        Assert.True(repository.Exists(roomId, period));
        var saved = repository.Get(roomId, period);
        Assert.NotNull(saved);
        Assert.Equal(100m, saved.PreviousWaterMeter.Value);
        Assert.Equal(110m, saved.CurrentWaterMeter.Value);
        Assert.Equal(500m, saved.PreviousElectricityMeter.Value);
        Assert.Equal(520m, saved.CurrentElectricityMeter.Value);
    }

    [Fact]
    public void Save_WhenReadingAlreadyExistsForPeriod_UpdatesSuccessfully()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(110m),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

        // Save again with updated current readings
        service.Save(
            roomId,
            period,
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(500m),
            MeterReading.Create(550m));

        var saved = repository.Get(roomId, period);
        Assert.NotNull(saved);
        Assert.Equal(125m, saved.CurrentWaterMeter.Value);
        Assert.Equal(550m, saved.CurrentElectricityMeter.Value);
    }

    [Fact]
    public void Save_WhenCurrentMeterLessThanPrevious_ThrowsInvalidMeterReadingException()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var service = new SaveMonthlyMeterReadingService(repository);
        var roomId = RoomId.New();
        var period = BillingPeriod.Create("2026-10");

        Assert.Throws<InvalidMeterReadingException>(() =>
            service.Save(
                roomId,
                period,
                MeterReading.Create(110m),
                MeterReading.Create(100m),
                MeterReading.Create(500m),
                MeterReading.Create(520m)));
    }
}
