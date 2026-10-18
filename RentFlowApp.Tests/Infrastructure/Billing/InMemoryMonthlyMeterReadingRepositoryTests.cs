using RentFlowApp.Domain.Billing;
using RentFlowApp.Domain.ValueObjects;
using RentFlowApp.Infrastructure.Billing;

namespace RentFlowApp.Tests.Infrastructure.Billing;

public class InMemoryMonthlyMeterReadingRepositoryTests
{
    [Fact]
    public void AddThenGet_ReturnsReadingForRoomAndPeriod()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var reading = CreateReading();

        repository.Add(reading);

        Assert.Same(reading, repository.Get(reading.RoomId, reading.BillingPeriod));
        Assert.True(repository.Exists(reading.RoomId, reading.BillingPeriod));
    }

    [Fact]
    public void Add_WhenRoomAndPeriodAlreadyExist_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var firstReading = CreateReading();
        var duplicateReading = MonthlyMeterReading.Create(
            firstReading.RoomId,
            firstReading.BillingPeriod,
            MeterReading.Create(200m),
            MeterReading.Create(225m),
            MeterReading.Create(2000m),
            MeterReading.Create(2010m));

        repository.Add(firstReading);

        Assert.Throws<InvalidOperationException>(() => repository.Add(duplicateReading));
    }

    [Fact]
    public void Get_WhenRoomAndPeriodDoNotExist_ReturnsNull()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();

        Assert.Null(repository.Get(
            RoomId.New(),
            BillingPeriod.Create("2026-10")));
    }

    [Fact]
    public void GetAll_ReturnsAllReadings()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var reading1 = CreateReading();
        var reading2 = MonthlyMeterReading.Create(
            RoomId.New(),
            BillingPeriod.Create("2026-10"),
            MeterReading.Create(50m),
            MeterReading.Create(75m),
            MeterReading.Create(500m),
            MeterReading.Create(520m));

        repository.Add(reading1);
        repository.Add(reading2);

        var all = repository.GetAll().ToList();
        Assert.Equal(2, all.Count);
        Assert.Contains(reading1, all);
        Assert.Contains(reading2, all);
    }

    [Fact]
    public void Update_WhenReadingExists_ReplacesReading()
    {
        var repository = new InMemoryMonthlyMeterReadingRepository();
        var reading = CreateReading();
        repository.Add(reading);

        var updatedReading = MonthlyMeterReading.Create(
            reading.RoomId,
            reading.BillingPeriod,
            reading.PreviousWaterMeter,
            MeterReading.Create(130m),
            reading.PreviousElectricityMeter,
            MeterReading.Create(1020m));

        repository.Update(updatedReading);

        var retrieved = repository.Get(reading.RoomId, reading.BillingPeriod);
        Assert.NotNull(retrieved);
        Assert.Equal(130m, retrieved.CurrentWaterMeter.Value);
        Assert.Equal(1020m, retrieved.CurrentElectricityMeter.Value);
    }

    private static MonthlyMeterReading CreateReading()
    {
        return MonthlyMeterReading.Create(
            RoomId.New(),
            BillingPeriod.Create("2026-10"),
            MeterReading.Create(100m),
            MeterReading.Create(125m),
            MeterReading.Create(1000m),
            MeterReading.Create(1010m));
    }
}
